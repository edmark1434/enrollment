$(document).ready(function () {
    $('#btn').click(function () {
        $('.input-error').removeClass('input-error');
        
        var newPass = $('#newPassword').val();
        var confirmPass = $('#confirmPass').val();
        if(newPass !== confirmPass) {
            Swal.fire({
                title: 'Warning',
                text: "Passwords don't match!",
                icon: 'warning',
                confirmButtonText: 'Try Again'
            });
            return;
        }
        var studCode = parseInt($('#studCode').val(), 10);
        
        if (isNaN(studCode)) {
            alert("Please enter a valid numeric student code.");
            return;
        }
        var student = {
            Stud_Code: studCode,
            Stud_Lname: $('#lastName').val(),
            Stud_Fname: $('#firstName').val(),
            Stud_Mname: $('#middleName').val(),
            Stud_Dob: $('#birthDate').val(),
            Stud_Contact: $('#contactNo').val(),
            Stud_Email: $('#emailAddress').val(),
            Stud_Address: $('#address').val(),
            Stud_Password: $('#newPassword').val(),
            ProgCode : $("#program").val()
        };
        console.log(student);
        $.ajax({
            type: "POST",
            url: '/Auth/Entry',
            contentType: 'application/json; charset=utf-8',
            data: JSON.stringify(student),
            success: function (response) {
                if (response.mess === 1) {
                    Swal.fire({
                        title: 'Success!',
                        text: response.message,
                        icon: 'success',
                        confirmButtonText: 'OK',
                        timer: 2000,
                        timerProgressBar: true,
                        willClose: () => {
                            window.location.href = response.redirectUrl;
                        }
                    });
                } else if (response.mess === 2 || response.mess === 3) {
                    Swal.fire({
                        title: 'Error',
                        text: response.error,
                        icon: 'error',
                        confirmButtonText: 'Try Again'
                    });
                    $('#' + response.field).addClass('input-error').focus();
                } else {
                    Swal.fire({
                        title: 'Error',
                        text: response.earror || "Submission failed.",
                        icon: 'error',
                        confirmButtonText: 'Try Again'
                    });
                }
            },
            error: function () {
                Swal.fire({
                    title: 'Error',
                    text: "Something went wrong during submission.",
                    icon: 'error',
                    confirmButtonText: 'Close'
                });
            }
        });
    });
});