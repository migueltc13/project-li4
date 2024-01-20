connection.on("UpdateAuction", (auctionId, bidValue, bidAmountString, bidPlaceholderString, buyerId, buyerUsername, buyerFullName_, bidTime) => {
    // Get the current auction ID from the request query string
    var currentAuctionId = parseInt(new URLSearchParams(window.location.search).get("id"), 10);
    if (currentAuctionId == auctionId) {
        // Update the current price
        document.getElementById("auctionPrice").innerText = bidAmountString;

        // Update the buyer
        var buyerLink = document.getElementById("buyerLink");
        var buyerFullName = document.getElementById("buyerFullName");

        buyerLink.href = "/account?id=" + buyerId;
        buyerLink.innerText = buyerUsername;
        buyerFullName.innerText = buyerFullName_;

        // Update the bid button placeholder
        document.getElementById("bidButton").innerText = bidPlaceholderString;

        // Update the bid button value if the current value is less than the new bid amount
        var bidButton = document.getElementById("bidButton");
        if (bidButton.value < bidValue) {
            bidButton.value = bidValue.toFixed(2);
        }

        // Update the bid history
        var bidHistory = document.getElementById('bidHistory');

        var li = document.createElement("li");
        li.innerHTML = "<p><a href='/account?id=" + buyerId + "'>" + buyerUsername + "</a> - " + bidAmountString + " - " + bidTime + "</p>";

        // Insert the new list item at the beginning of the bid history
        bidHistory.insertBefore(li, bidHistory.firstChild);
    }
});

connection.on("RefreshAuction", (auctionId) => {
    currentAuctionId = parseInt(new URLSearchParams(window.location.search).get("id"), 10);
    if (currentAuctionId == auctionId) {
        window.location.href = window.location.href;
    }
});