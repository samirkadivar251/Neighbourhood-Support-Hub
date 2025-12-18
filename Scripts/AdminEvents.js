$(document).ready(function () {
    $(".approve-btn").click(function () {
        var eventId = $(this).data("id");
        $.post("/Admin/ApproveEvent", { id: eventId }, function (response) {
            if (response.success) {
                alert("Event approved successfully!");
                location.reload();
            } else {
                alert(response.message);
            }
        });
    });

    $(".reject-btn").click(function () {
        var eventId = $(this).data("id");
        $.post("/Admin/RejectEvent", { id: eventId }, function (response) {
            if (response.success) {
                alert("Event rejected!");
                location.reload();
            } else {
                alert(response.message);
            }
        });
    });

    $(".delete-btn").click(function () {
        var eventId = $(this).data("id");
        $.post("/Admin/DeleteEvent", { id: eventId }, function (response) {
            if (response.success) {
                alert("Event deleted!");
                location.reload();
            } else {
                alert(response.message);
            }
        });
    });
});
