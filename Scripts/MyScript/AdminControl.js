$().ready(function(){
   $('#btnEnroll').click(function(){
       var acadYear = $('#currentAy').val();
       var sem = $('#currentSemester').val();
       var isActive = $('#isActive').is(':checked');
       var value = !!isActive;
       var data = {
           ay_code : acadYear,
           sem_id : sem,
           is_open : value
       }
       $.ajax({
          url: 'updateCurrentEnrollments' ,
          method:'POST', 
          data : data,
          success: function(response){
              if(response.success){
                  setTimeout(()=>{
                      swal.fire({
                          title: 'Successfully updated!',
                          icon: 'success',
                          confirmButtonText: 'OK'
                      });
                  },3000)
                  window.location.href = response.redirectUrl;
              }
                  swal.fire({
                      title: 'Cannot update settings!',
                      icon: 'warning',
                      confirmButtonText: 'TRY AGAIN'
                  });
          },
          error: function(xhr){
              console.log(xhr);
          } 
       });
   }); 
});