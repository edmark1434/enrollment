$(document).ready(function () {
    // Initialize Edit Modal
    $(document).on('click', '.edit-course', function () {
        const $btn = $(this);
        const code = $btn.data('code');
        const title = $btn.data('title');
        const prereq = $btn.data('prereq') || '';
        const units = $btn.data('units');
        const lec = $btn.data('lec');
        const lab = $btn.data('lab');

        $('#editCrsCode').val(code);
        $('#editCrsTitle').val(title);
        $('#editPreqCrsCode').val(prereq);
        $('#editCrsUnits').val(units);
        $('#editCrsLec').val(lec);
        $('#editCrsLab').val(lab);

        $('#editCourseModal').modal('show');
    });

    // Handle Delete
    $(document).on('click', '.delete-course', function () {
        const courseId = $(this).data('code');
        confirmDelete(courseId);
    });
    function loadPrerequisites() {
        $.get('/Course/GetAllCoursesForDropdown', function (data) {
            data.forEach(item => {
                $('#editPreqCrsCode').append(`<option value="${item.id}">${item.text}</option>`);
            });
            $('#editPreqCrsCode').select2({ data: data });
        });
    }

    loadPrerequisites();
});