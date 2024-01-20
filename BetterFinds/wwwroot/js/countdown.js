// Countdown function for auction page
function countdown() {
    // Get the current time
    var now = new Date().getTime();

    // Calculate the remaining time in milliseconds
    var distance = endTime - now;

    // Calculate days, hours, minutes, and seconds
    var days = Math.floor(distance / (1000 * 60 * 60 * 24));
    var hours = Math.floor((distance % (1000 * 60 * 60 * 24)) / (1000 * 60 * 60));
    var minutes = Math.floor((distance % (1000 * 60 * 60)) / (1000 * 60));
    var seconds = Math.floor((distance % (1000 * 60)) / 1000);

    // Format the countdown for display
    var countdownText = 'Ends in: ';
    if (days > 0) {
        countdownText += days + "d ";
    }
    if (hours > 0 || days > 0) {
        countdownText += hours + "h ";
    }
    if (minutes > 0 || hours > 0 || days > 0) {
        countdownText += minutes + "m ";
    }
    countdownText += seconds + "s";

    // Display the countdown in the "countdown" div
    document.getElementById("countdown").innerHTML = countdownText;

    // If the countdown is over, display a message
    if (distance < 0) {
        clearInterval(x);
        document.getElementById("countdown").innerHTML = "Auction has ended!";
    }
}

// Call the countdown function immediately
countdown();

// Update the countdown every second
var x = setInterval(countdown, 1000); // Update every 1000 milliseconds (1 second)