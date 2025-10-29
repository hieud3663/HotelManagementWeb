function saveQuickCustomer() {
    var form = $('#quickCustomerForm');
    var isValid = true;

    // Validate Full Name
    var fullName = form.find('[name="FullName"]').val().trim();
    if (!fullName) {
        displayError("fullNameError", "Họ và tên không được để trống.");
        isValid = false;
    } else {
        clearError("fullNameError");
    }

    // Validate Phone Number
    var phoneNumber = form.find('[name="PhoneNumber"]').val().trim();
    if (!validatePhoneNumber(phoneNumber)) {
        displayError("phoneNumberError", "Số điện thoại phải bắt đầu bằng 0 và có 10 chữ số.");
        isValid = false;
    } else {
        clearError("phoneNumberError");
    }

    // Validate CCCD
    var idCardNumber = form.find('[name="IdCardNumber"]').val().trim();
    if (!validateCCCD(idCardNumber)) {
        displayError("cccdError", "Số CCCD phải có đúng 12 chữ số.");
        isValid = false;
    } else {
        clearError("cccdError");
    }

    // Validate Age
    var dob = form.find('[name="Dob"]').val();
    if (!validateAge(dob)) {
        displayError("dobError", "Khách hàng phải từ 18 tuổi trở lên.");
        isValid = false;
    } else {
        clearError("dobError");
    }

    if (!isValid) {
        return;
    }

    var formData = {
        FullName: fullName,
        Gender: form.find('[name="Gender"]').val(),
        PhoneNumber: phoneNumber,
        Email: form.find('[name="Email"]').val(),
        IdCardNumber: idCardNumber,
        Dob: dob,
        Address: form.find('[name="Address"]').val()
    };

    var token = $('input[name="__RequestVerificationToken"]').val();
    $.ajax({
        url: '/Customer/QuickCreate',
        type: 'POST',
        headers: {
            'RequestVerificationToken': token
        },
        data: formData,
        success: function(response) {
            if (response.success) {
                var newOption = new Option(response.customerName, response.customerId, true, true);
                $('#CustomerID').append(newOption).trigger('change');
                
                $('#addCustomerModal').modal('hide');
                form[0].reset();
                
                alert('Thêm khách hàng thành công!');
            } else {
                alert('Lỗi: ' + response.message);
            }
        },
        error: function() {
            alert('Có lỗi xảy ra khi thêm khách hàng!');
        }
    });
}