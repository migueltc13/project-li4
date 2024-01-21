connection.on("UpdateNotifications", (newCount, clientId) => {
    if (clientId == currentClientId) {
        // Update the notification count in the UI
        document.getElementById("notificationCount").innerText = newCount;

        // Refresh the notifications page when a notification is received
        console.log(currentPageName);
        if ("/Notifications" == currentPageName) {
            window.location.href = window.location.href;
        }
    }
});