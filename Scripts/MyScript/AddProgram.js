$(document).ready(function () {
    $('#prerequisite').select2({
        theme: 'bootstrap-5',
        placeholder: 'Search or select prerequisites',
        width: '100%'
    });

    $('#courseCategory').on('change', function () {
        const selectedCode = $(this).find('option:selected').data('code');
        $('#courseCodePrefix').text(selectedCode);
        updateFullCourseCodePreview();
    });

    $('#courseCode').on('input', function () {
        updateFullCourseCodePreview();
    });

    function updateFullCourseCodePreview() {
        const prefix = $('#courseCodePrefix').text().replace(/\s/g, '');
        const inputCode = $('#courseCode').val().trim();
        $('#fullCourseCodePreview').text(prefix + inputCode);
    }

    $('#lecUnits').on('input', function () {
        if ($('#lecUnits').val() && $('#labUnits').val()) {
            $('#lecLabSame').removeAttr('hidden');
        } else {
            $('#lecLabSame').attr('hidden', true);
        }
    });

    $('#labUnits').on('input', function () {
        if ($('#lecUnits').val() && $('#labUnits').val()) {
            $('#lecLabSame').removeAttr('hidden');
        } else {
            $('#lecLabSame').attr('hidden', true);
        }
    });

    $('#addCourseForm').submit(function (e) {
        e.preventDefault();

        if (!$('#lecUnits').val() && !$('#labUnits').val()) {
            showError('Please enter the number of hours for the course.');
            return;
        }

        if (!this.checkValidity()) {
            $(this).addClass('was-validated');
            return;
        }

        let coursesToAdd = [];
        const isLecLabSame = $('#lecLabSame').is(':checked');

        if (isLecLabSame) {
            coursesToAdd.push({
                Crs_Code: $('#courseCategory').val() + $('#courseCode').val(),
                Crs_Title: $('#descriptiveTitle').val() + ' (LEC)',
                Crs_Units: $('#numberOfUnits').val(),
                Crs_Lec: $('#lecUnits').val(),
                Crs_Lab: 0,
                Ctg_Code: $('#courseCategory').val(),
                Preq_Crs_Code: $('#prerequisite').val() ? $('#prerequisite').val().join(',') : null,
            });

            coursesToAdd.push({
                Crs_Code: $('#courseCategory').val() + $('#courseCode').val() + 'L',
                Crs_Title: $('#descriptiveTitle').val() + ' (LAB)',
                Crs_Units: $('#numberOfUnits').val(),
                Crs_Lec: 0,
                Crs_Lab: $('#labUnits').val(),
                Ctg_Code: $('#courseCategory').val(),
                Preq_Crs_Code: $('#prerequisite').val() ? $('#prerequisite').val().join(',') : null,
            });
        } else {
            coursesToAdd.push({
                Crs_Code: $('#courseCategory').val() + $('#courseCode').val(),
                Crs_Title: $('#descriptiveTitle').val(),
                Crs_Units: $('#numberOfUnits').val(),
                Crs_Lec: $('#lecUnits').val() || 0,
                Crs_Lab: $('#labUnits').val() || 0,
                Ctg_Code: $('#courseCategory').val(),
                Preq_Crs_Code: $('#prerequisite').val() ? $('#prerequisite').val().join(',') : null,
            });
        }

        Swal.fire({
            title: 'Processing',
            html: 'Please wait while we add the course...',
            allowOutsideClick: false,
            didOpen: () => Swal.showLoading()
        });

        let completedRequests = 0;
        let hasErrors = false;
        let errorMessage = "";

        function checkCompletion() {
            if (completedRequests === coursesToAdd.length) {
                Swal.close();
                if (hasErrors) {
                    showError(errorMessage);
                } else {
                    showSuccess('Course(s) added successfully!');
                    $('#addCourseForm')[0].reset();
                    setTimeout(()=>{
                        window.location.href = '/Admin/Admin_Course';
                    },2000);
                }
            }
        }

        const ajaxUrl = $('#addCourseForm').data('ajax-url');

        coursesToAdd.forEach(function (courseData) {
            $.ajax({
                url: ajaxUrl,
                type: 'POST',
                data: courseData,
                contentType: 'application/x-www-form-urlencoded',
                success: function (response) {
                    completedRequests++;
                    if (!response.success) {
                        hasErrors = true;
                        errorMessage = response.message;
                    }
                    checkCompletion();
                },
                error: function (xhr) {
                    completedRequests++;
                    hasErrors = true;
                    errorMessage = handleAjaxError(xhr);
                    checkCompletion();
                }
            });
        });

        function handleAjaxError(xhr) {
            let errorMessage = 'An error occurred while adding the course';

            if (xhr.responseJSON) {
                if (xhr.responseJSON.errors) {
                    $.each(xhr.responseJSON.errors, function (field, messages) {
                        if (field === 'Crs_Code') {
                            $('#courseCode').addClass('is-invalid');
                            $('#courseCode').next('.invalid-feedback').text(messages[0]);
                        }
                        if (field === 'Crs_Title') {
                            $('#descriptiveTitle').addClass('is-invalid');
                            $('#descriptiveTitle').next('.invalid-feedback').text(messages[0]);
                        }
                    });
                    errorMessage = "Please correct the highlighted errors.";
                } else if (xhr.responseJSON.message) {
                    errorMessage = xhr.responseJSON.message;
                }
            }

            return errorMessage;
        }

        function showSuccess(message) {
            Swal.fire({
                icon: 'success',
                title: 'Success!',
                text: message,
                confirmButtonText: 'OK'
            });
        }

        function showError(message) {
            Swal.fire({
                icon: 'error',
                title: 'Error',
                text: message,
                confirmButtonText: 'Try Again'
            });
        }

        $('#courseCode, #descriptiveTitle').on('input', function () {
            $(this).removeClass('is-invalid');
        });
    });
});