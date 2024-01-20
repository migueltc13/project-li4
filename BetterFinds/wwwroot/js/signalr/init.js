const connection = new signalR.HubConnectionBuilder()
    .withUrl("/notificationHub")
    .build();

connection.start().then(() => {
    console.log("Connected to notification hub");
}).catch(err => console.error(err));