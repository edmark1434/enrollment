$().ready(function () {
    const scheduleBody = $('#scheduleBody');
    let editingRow = null;

    // Format time to AM/PM
    function formatTime(time) {
        let [hours, minutes] = time.split(":");
        hours = parseInt(hours);
        const period = hours >= 12 ? "PM" : "AM";
        hours = hours % 12 || 12;
        return `${hours}:${minutes} ${period}`;
    }

    // Group by time slots
    function formatDayTime(details) {
        const timeGroups = {};
        details.forEach(detail => {
            const key = `${detail.startTime}-${detail.endTime}`;
            if (!timeGroups[key]) timeGroups[key] = [];
            timeGroups[key].push(detail.day);
        });

        return Object.entries(timeGroups).map(([key, days]) => {
            const [start, end] = key.split("-");
            return `${days.join("/")} ${formatTime(start)} - ${formatTime(end)}`;
        }).join(" / ");
    }

    // Load sample data into table
    function loadAndRenderSchedule() {
        const testData = [
            {
                misCode: "MIS102",
                course: "DS102",
                courseName: "Data Structures",
                room: "R102",
                professor: "Dr. Sarah Lee",
                section: "CS-1B",
                day: "Wednesday",
                startTime: "13:00",
                endTime: "14:30"
            },
            {
                misCode: "MIS102",
                course: "DS102",
                courseName: "Data Structures",
                room: "R102",
                professor: "Dr. Sarah Lee",
                section: "CS-1B",
                day: "Friday",
                startTime: "15:00",
                endTime: "16:30"
            }
        ];

        const grouped = groupSchedules(testData);
        scheduleBody.empty();

        $.each(grouped, function (_, schedule) {
            const dayTimeDisplay = formatDayTime(schedule.scheduleDetails);
            const row = $(
                `<tr data-id="${schedule.misCode}">
                    <td>${schedule.misCode}</td>
                    <td data-crs="${schedule.course}" 
                        data-days='${JSON.stringify(schedule.scheduleDetails.map(d => d.day))}'
                        data-starts='${JSON.stringify(schedule.scheduleDetails.map(d => d.startTime))}'
                        data-ends='${JSON.stringify(schedule.scheduleDetails.map(d => d.endTime))}'>
                        ${schedule.courseName} (${schedule.course})
                    </td>
                    <td>${schedule.scheduleDetails.map(d => d.day).join(", ")}</td>
                    <td>${dayTimeDisplay}</td>
                    <td>${schedule.room}</td>
                    <td>${schedule.professor}</td>
                    <td>${schedule.section}</td>
                    <td class="actions">
                        <button class="edit-schedule btn btn-sm btn-primary">Edit</button>
                        <button class="delete-schedule btn btn-sm btn-danger">Delete</button>
                    </td>
                </tr>`
            );
            scheduleBody.append(row);
        });
    }

    // Group schedules by misCode
    function groupSchedules(schedules) {
        const grouped = {};
        schedules.forEach(schedule => {
            if (!grouped[schedule.misCode]) {
                grouped[schedule.misCode] = {
                    misCode: schedule.misCode,
                    course: schedule.course,
                    courseName: schedule.courseName,
                    room: schedule.room,
                    professor: schedule.professor,
                    section: schedule.section,
                    scheduleDetails: []
                };
            }
            grouped[schedule.misCode].scheduleDetails.push({
                day: schedule.day,
                startTime: schedule.startTime,
                endTime: schedule.endTime
            });
        });
        return Object.values(grouped);
    }

    // Open modal
    function openModal(mode, row = null) {
        $('#modal').addClass('active');
        editingRow = row;
        $('#modalTitle').text(mode === "add" ? "Add New Schedule" : "Edit Schedule");

        $('#scheduleForm')[0].reset();
        $('#dayTimeContainer').html(`
            <div class="day-time-group">
                <select class="day-select">
                    <option value="">Day</option>
                    <option value="Monday">Monday</option>
                    <option value="Tuesday">Tuesday</option>
                    <option value="Wednesday">Wednesday</option>
                    <option value="Thursday">Thursday</option>
                    <option value="Friday">Friday</option>
                    <option value="Saturday">Saturday</option>
                    <option value="Sunday">Sunday</option>
                </select>
                <input type="time" class="start-input" step="1800" />
                <input type="time" class="end-input" step="1800" />
                <span class="remove-day-time">×</span>
            </div>
        `);

        if (mode === "edit" && row) {
            const cells = $(row).find("td");
            $('#course').val($(cells[1]).data('crs'));
            $('#professor').val($(cells[5]).text().trim());
            $('#room').val($(cells[4]).text().trim());
            $('#section').val($(cells[6]).text().trim());

            const days = JSON.parse($(cells[1]).data('days') || '[""]');
            const starts = JSON.parse($(cells[1]).data('starts') || '[""]');
            const ends = JSON.parse($(cells[1]).data('ends') || '[""]');
            $('#dayTimeContainer').empty();

            days.forEach((day, i) => {
                const group = $(
                    `<div class="day-time-group">
                        <select class="day-select">
                            <option value="">Day</option>
                            <option value="Monday" ${day === 'Monday' ? 'selected' : ''}>Monday</option>
                            <option value="Tuesday" ${day === 'Tuesday' ? 'selected' : ''}>Tuesday</option>
                            <option value="Wednesday" ${day === 'Wednesday' ? 'selected' : ''}>Wednesday</option>
                            <option value="Thursday" ${day === 'Thursday' ? 'selected' : ''}>Thursday</option>
                            <option value="Friday" ${day === 'Friday' ? 'selected' : ''}>Friday</option>
                            <option value="Saturday" ${day === 'Saturday' ? 'selected' : ''}>Saturday</option>
                            <option value="Sunday" ${day === 'Sunday' ? 'selected' : ''}>Sunday</option>
                        </select>
                        <input type="time" class="start-input" step="1800" value="${starts[i] || ''}" />
                        <input type="time" class="end-input" step="1800" value="${ends[i] || ''}" />
                        <span class="remove-day-time">×</span>
                    </div>`
                );
                $('#dayTimeContainer').append(group);
            });
        }
    }

    function closeModal() {
        $('#modal').removeClass('active');
        editingRow = null;
    }

    $('#closeModalBtn').on('click', closeModal);

    $('#addAnotherDayTime').on('click', function () {
        const group = $(
            `<div class="day-time-group">
                <select class="day-select">
                    <option value="">Day</option>
                    <option value="Monday">Monday</option>
                    <option value="Tuesday">Tuesday</option>
                    <option value="Wednesday">Wednesday</option>
                    <option value="Thursday">Thursday</option>
                    <option value="Friday">Friday</option>
                    <option value="Saturday">Saturday</option>
                    <option value="Sunday">Sunday</option>
                </select>
                <input type="time" class="start-input" step="1800" />
                <input type="time" class="end-input" step="1800" />
                <span class="remove-day-time">×</span>
            </div>`
        );
        $('#dayTimeContainer').append(group);
    });

    $('#dayTimeContainer').on('click', '.remove-day-time', function () {
        if ($('.day-time-group').length > 1) {
            $(this).closest('.day-time-group').remove();
        } else {
            alert("At least one day/time is required.");
        }
    });

    $('#scheduleForm').on('submit', function (e) {
        e.preventDefault();

        const course = $('#course').val();
        const professor = $('#professor').val();
        const room = $('#room').val();
        const section = $('#section').val();

        const days = [], starts = [], ends = [];

        $('.day-time-group').each(function () {
            const day = $(this).find('.day-select').val();
            const start = $(this).find('.start-input').val();
            const end = $(this).find('.end-input').val();
            if (day && start && end) {
                days.push(day);
                starts.push(start);
                ends.push(end);
            }
        });

        const newRow = $('<tr>').attr('data-id', editingRow ? $(editingRow).data('id') : 'MIS' + Math.floor(Math.random() * 1000));
        const dayDisplay = days.join(", ");
        const timeDisplay = Object.keys(days).length === 1
            ? `${formatTime(starts[0])} - ${formatTime(ends[0])}`
            : Object.entries(days).map(i => `${days[i]} ${formatTime(starts[i])} - ${formatTime(ends[i])}`).join(" / ");

        newRow.html(`
            <td>${newRow.data('id')}</td>
            <td data-crs="${course}"
                data-days='${JSON.stringify(days)}'
                data-starts='${JSON.stringify(starts)}'
                data-ends='${JSON.stringify(ends)}'>
                ${$('#course option:selected').text()}
            </td>
            <td>${dayDisplay}</td>
            <td>${timeDisplay}</td>
            <td>${room}</td>
            <td>${professor}</td>
            <td>${section}</td>
            <td class="actions">
                <button class="edit-schedule btn btn-sm btn-primary">Edit</button>
                <button class="delete-schedule btn btn-sm btn-danger">Delete</button>
            </td>
        `);

        if (editingRow) {
            $(editingRow).replaceWith(newRow);
        } else {
            scheduleBody.append(newRow);
        }

        closeModal();
    });

    $(document).on('click', '.edit-schedule', function () {
        openModal('edit', $(this).closest('tr'));
    });

    $(document).on('click', '.delete-schedule', function () {
        if (confirm("Are you sure?")) {
            $(this).closest('tr').remove();
        }
    });

    $('#filterButton').on('click', function () {
        const year = $('#academicYearFilter').val().toLowerCase();
        const program = $('#programFilter').val().toLowerCase();
        const section = $('#sectionFilter').val().toLowerCase();
        const semester = $('#semesterFilter').val().toLowerCase();

        $('#scheduleBody tr').each(function () {
            const cells = $(this).find("td");
            const show =
                (!year || cells.eq(1).text().toLowerCase().includes(year)) &&
                (!program || cells.eq(1).text().toLowerCase().includes(program)) &&
                (!section || cells.eq(6).text().toLowerCase().includes(section)) &&
                (!semester || true); // Simulated
            $(this).toggle(show);
        });
    });

    $('#resetFilterButton').on('click', function () {
        $('#academicYearFilter, #programFilter, #sectionFilter, #semesterFilter').val('');
        $('#scheduleBody tr').show();
    });

    $(function () {
        loadAndRenderSchedule();
    });
});
    