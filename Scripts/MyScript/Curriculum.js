$(document).ready(function () {
    const $assignCourseModal = $('#assignCourseModal');
    const $modalProgramName = $('#modalProgramName');
    const $modalYearSemester = $('#modalYearSemester');
    const $availableCoursesList = $('#availableCoursesList');
    const $courseSearch = $('#courseSearch');
    const $confirmAssign = $('#confirmAssign');
    const $programSelect = $('#programSelect');
    const $yearLevelSelect = $('#yearLevelSelect');
    const $semesterSelect = $('#semesterSelect');
    const $academicYearSelect = $('#academicYearSelect');
    const $filterProgram = $('#filterProgram');
    const $filterAY = $('#filterAY');

    let selectedCoursesToAssign = new Set();
    let allAvailableCourses = [];

    // Load programs and academic years independently
    function loadPrograms() {
        $.ajax({
            url: '/CurriculumCourse/GetAllPrograms',
            method: 'GET',
            success: function (data) {
                var options = '<option value="">All Programs</option>';
                if (data && data.length > 0) {
                    $.each(data, function (i, prog) {
                        options += `<option value="${prog.ProgCode}">${prog.ProgTitle}</option>`;
                    });
                } else {
                    options = '<option value="">No programs found</option>';
                }
                $programSelect.html(options);
                $filterProgram.html(options);
            },
            error: function () {
                alert("Failed to fetch programs.");
            }
        });
    }

    function loadAcademicYears() {
        $.ajax({
            url: '/CurriculumCourse/GetAllAcademicYears',
            method: 'GET',
            success: function (data) {
                var options = '<option value="">All Years</option>';
                if (data && data.length > 0) {
                    $.each(data, function (i, ay) {
                        options += `<option value="${ay.AyCode}">${ay.DisplayText}</option>`;
                    });
                } else {
                    options = '<option value="">No academic years found</option>';
                }
                $academicYearSelect.html(options);
                $filterAY.html(options);
            },
            error: function () {
                alert("Failed to fetch academic years.");
            }
        });
    }

    function loadAssignedCurriculum() {
        applyFilters(); // Just trigger filter to load all data
    }

    function applyFilters() {
        const selectedProg = $('#filterProgram').val(); // can be empty
        const selectedSemester = $('#filterSemester').val(); // "1", "2" or ""
        const selectedAY = $('#filterAY').val(); // can be empty

        $.get('/CurriculumCourse/GetFilteredCurriculum', {
            progCode: selectedProg || "",
            semester: selectedSemester || "",
            ayCode: selectedAY || ""
        }, function (data) {
            $('#assignedCurriculumTable tbody').empty();

            if (!data || data.length === 0) {
                $('#assignedCurriculumTable tbody').append(
                    '<tr><td colspan="5" class="text-center">No matching curriculum found.</td></tr>'
                );
                return;
            }

            $.each(data, function (i, item) {
                $('#assignedCurriculumTable tbody').append(`
                <tr>
                    <td>${item.progCode}</td>
                    <td>${item.crsCode}</td>
                    <td>${item.curYearLevel}</td>
                    <td>${item.curSemester === 1 ? '1st Semester' : item.curSemester === 2 ? '2nd Semester' : 'Unknown'}</td>
                    <td>${item.ayDisplay}</td>
                </tr>
            `);
            });
        }).fail(function () {
            alert('⚠️ Failed to apply filters.');
        });
    }

    function fetchCourses() {
        const selectedProg = $programSelect.val();
        const selectedYear = $yearLevelSelect.val();
        const selectedSemester = $semesterSelect.val(); // e.g., "1st Semester"
        const selectedAY = $academicYearSelect.val();

        let semesterValue = 0;
        if (selectedSemester === "1st Semester") semesterValue = 1;
        else if (selectedSemester === "2nd Semester") semesterValue = 2;

        $availableCoursesList.empty().append('<tr><td colspan="8" class="text-center">Loading courses...</td></tr>');

        return $.when(
            $.get('/Course/GetAllCourses'),
            $.get('/CurriculumCourse/GetAssignedCourses', {
                progCode: selectedProg,
                yearLevel: selectedYear,
                semester: semesterValue.toString(), // send as string for compatibility
                ayCode: selectedAY
            })
        ).done(function (allCoursesRes) {
            allAvailableCourses = allCoursesRes[0]; // unwrap jQuery response array
            selectedCoursesToAssign = new Set(allAvailableCourses); // Restore checked courses
            renderFilteredCourses();
        }).fail(function () {
            $availableCoursesList.empty().append('<tr><td colspan="8" class="text-center text-danger">Failed to load courses.</td></tr>');
        });
    }

    function renderFilteredCourses() {
        const query = $courseSearch.val().toLowerCase();
        const filtered = allAvailableCourses.filter(c =>
            c.code.toLowerCase().includes(query) || c.title.toLowerCase().includes(query)
        );

        const selected = filtered.filter(c => selectedCoursesToAssign.has(c.code));
        const unselected = filtered.filter(c => !selectedCoursesToAssign.has(c.code));

        $availableCoursesList.empty();

        selected.forEach(course => {
            $availableCoursesList.append(createCourseRow(course, true));
        });

        if (selected.length && unselected.length) {
            $availableCoursesList.append(`<tr><td colspan="8" style="border-top: 2px solid #007bff;"></td></tr>`);
        }

        unselected.forEach(course => {
            $availableCoursesList.append(createCourseRow(course, false));
        });
    }

    function createCourseRow(course, isChecked) {
        return $(`
            <tr>
                <td><input type="checkbox" class="course-checkbox" data-code="${course.code}" ${isChecked ? 'checked' : ''}></td>
                <td>${course.code}</td>
                <td>${course.title}</td>
                <td>${course.category || ''}</td>
                <td>${course.prerequisite || 'None'}</td>
                <td>${course.units !== undefined ? course.units : ''}</td>
                <td>${course.lec !== undefined ? course.lec : ''}</td>
                <td>${course.lab !== undefined ? course.lab : ''}</td>
            </tr>
        `);
    }

    // Show modal setup
    $assignCourseModal.on('show.bs.modal', function () {
        const selectedProg = $programSelect.val();
        const selectedYear = $yearLevelSelect.val();
        const selectedSemester = parseInt($semesterSelect.val());
        const selectedAY = $academicYearSelect.val();

        if (!selectedProg || !selectedYear || !selectedSemester || !selectedAY) {
            alert('Please select Program, Year Level, Semester, and Academic Year.');
            return false;
        }
        var semester_name = selectedSemester === 1 ? "1st Semester" : "2nd Semester";
        $modalProgramName.text(selectedProg);
        $modalYearSemester.text(`${selectedYear} - ${semester_name}`);
        $courseSearch.val('');
        fetchCourses();
    });

    // Course search filter
    $courseSearch.on('input', function () {
        renderFilteredCourses();
    });

    // Track checkbox changes
    $availableCoursesList.on('change', '.course-checkbox', function () {
        const code = $(this).data('code');
        if ($(this).is(':checked')) {
            selectedCoursesToAssign.add(code);
        } else {
            selectedCoursesToAssign.delete(code);
        }
        renderFilteredCourses();
    });

    // Confirm assign button click
    $confirmAssign.on('click', function () {
        const selectedProg = $programSelect.val();
        const selectedYear = $yearLevelSelect.val();
        const selectedSemester = parseInt($semesterSelect.val()); // e.g., "1st Semester"
        const selectedAY = $academicYearSelect.val();

        const selectedCourses = [];
        $availableCoursesList.find('input.course-checkbox:checked').each(function () {
            selectedCourses.push($(this).data('code'));
        });

        if (selectedCourses.length === 0) {
            alert('Please select at least one course to assign.');
            return;
        }

        const dataToSend = selectedCourses.map(courseCode => ({
            curCode: `CURR-${selectedProg}-${selectedAY.split('-')[0]}-${courseCode}`,
            crsCode: courseCode,
            curYearLevel: selectedYear,
            curSemester: selectedSemester,
            ayCode: selectedAY,
            progCode: selectedProg
        }));

        $.ajax({
            url: '/CurriculumCourse/AssignCourses',
            method: 'POST',
            contentType: 'application/json',
            data: JSON.stringify(dataToSend),
            success: function (response) {
                if (response.success) {
                    Swal.fire({
                        title: 'Success!',
                        text: 'Courses have been assigned.',
                        icon: 'success',
                        timer: 2000,
                        timerProgressBar: true
                    });
                    $assignCourseModal.modal('hide');
                    loadAssignedCurriculum();
                } else {
                    Swal.fire({
                        title: 'Warning',
                        text: response.message,
                        icon: 'warning'
                    });
                }
            },
            error: function () {
                Swal.fire({
                    title: 'Error',
                    text: 'Unable to assign courses.',
                    icon: 'error'
                });
            }
        });
    });

    // Initial loads
    loadPrograms();
    loadAcademicYears();
    loadAssignedCurriculum();
    applyFilters();
    // Event listeners for filters
    $('#filterProgram, #filterSemester, #filterAY').on('change', applyFilters);
});