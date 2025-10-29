// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
// Function to validate phone number format
function validatePhoneNumber(phoneNumber) {
    const phoneRegex = /^0\d{9}$/; 
    return phoneRegex.test(phoneNumber);
}

// Function to validate age (>= 18)
function validateAge(dob) {
    const birthDate = new Date(dob);
    const today = new Date();
    const age = today.getFullYear() - birthDate.getFullYear();
    const monthDiff = today.getMonth() - birthDate.getMonth();
    if (monthDiff < 0 || (monthDiff === 0 && today.getDate() < birthDate.getDate())) {
        return age - 1 >= 18;
    }
    return age >= 18;
}

// Function to validate CCCD (12 digits)
function validateCCCD(cccd) {
    const cccdRegex = /^\d{12}$/;
    return cccdRegex.test(cccd);
}

// Function to display error messages
function displayError(elementId, message) {
    const errorElement = document.getElementById(elementId);
    if (errorElement) {
        errorElement.innerText = message;
        errorElement.style.display = "block";
    }
}

// Function to clear error messages
function clearError(elementId) {
    const errorElement = document.getElementById(elementId);
    if (errorElement) {
        errorElement.innerText = "";
        errorElement.style.display = "none";
    }
}

// Event listener for form submission
function initFormValidattion(id_form) {
    const form = document.getElementById(id_form);
    if (form) {
        form.addEventListener("submit", function (event) {
            let isValid = true;

            // Validate phone number
            const phoneNumberInput = document.querySelector("input[name='PhoneNumber']");
            if (phoneNumberInput) {
                const phoneNumber = phoneNumberInput.value.trim();
                if (!validatePhoneNumber(phoneNumber)) {
                    displayError("phoneNumberError", "Số điện thoại phải bắt đầu bằng 0 và có 10 chữ số.");
                    isValid = false;
                } else {
                    clearError("phoneNumberError");
                }
            }

            // Validate age
            const dobInput = document.querySelector("input[name='Dob']");
            if (dobInput) {
                const dob = dobInput.value.trim();
                if (!validateAge(dob)) {
                    displayError("dobError", "Tuổi phải từ 18 trở lên.");
                    isValid = false;
                } else {
                    clearError("dobError");
                }
            }

            // Validate CCCD
            const cccdInput = document.querySelector("input[name='IdCardNumber']");
            if (cccdInput) {
                const cccd = cccdInput.value.trim();
                if (!validateCCCD(cccd)) {
                    displayError("cccdError", "Số CCCD phải có đúng 12 chữ số.");
                    isValid = false;
                } else {
                    clearError("cccdError");
                }
            }

            // Prevent form submission if validation fails
            if (!isValid) {
                event.preventDefault();
            }
        });
    }
}

function validateReservationForm(event) {
    var checkInDate = new Date($('input[name="CheckInDate"]').val());
    var checkOutDate = new Date($('input[name="CheckOutDate"]').val());
    var now = new Date();

    var isValid = true;

    $('#checkInDateError').text('');
    $('#checkOutDateError').text('');

    if (checkInDate < now) {
        $('#checkInDateError').text('Ngày nhận phòng phải lớn hơn hoặc bằng ngày hiện tại.');
        isValid = false;
    }

    if (checkOutDate <= checkInDate) {
        $('#checkOutDateError').text('Ngày trả phòng phải lớn hơn ngày nhận phòng.');
        isValid = false;
    }

    if (!isValid) {
        event.preventDefault();
    }
}

function initEmployeeFormValidation() { 

    

}