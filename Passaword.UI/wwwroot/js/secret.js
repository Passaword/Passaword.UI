jQuery(document).ready(function($) {
    $("form").submit(function (e) {
        var $form = $(this);
        if ($form.valid()) {
            e.preventDefault();

            $.ajax({
                url: $form.attr("action"),
                dataType: 'json',
                method: 'post',
                data: $form.serialize(),
                success: function (secret) {
                    $(".alert-success").removeClass("hidden");
                    $(".alert-danger").addClass("hidden");
                    $form.addClass("hidden");
                    $("#secret").val(secret);
                },
                error: function() {
                    $(".alert-danger").removeClass("hidden");
                }
            });
        }
    });
});