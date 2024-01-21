connection.on("UpdateAuction", (auctionId, bidValue, bidAmountString, bidPlaceholderString, buyerId, buyerUsername, buyerFullName_, bidTime, sellerId) => {
    // Get the current auction ID from the request query string
    var currentAuctionId = parseInt(new URLSearchParams(window.location.search).get("id"), 10);
    if (currentAuctionId != NaN && currentAuctionId == auctionId) {
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
    else if (currentPageName == "/Index" || currentPageName == "/Search") {
        console.log("Index or Search");
        var updatedAuctions = document.getElementById("updatedAuctions");
        // Notify the user that there was an update to the auctions in the /Index or /Search pages
        updatedAuctions.innerHTML = "There was an update to the auctions. Please reload the page to see the changes.";
    }
    else if (currentPageName == "/MyAuctions") {
        // check if current client is the seller of the auction
        if (currentClientId == sellerId) {
            // Notify the user that there was an update to the auctions in the /MyAuctions page
            var updatedAuctions = document.getElementById("updatedAuctions");
            updatedAuctions.innerHTML = "There was an update to one or more of your auctions. Please reload the page to see the changes.";
        }
    }
});

connection.on("RefreshAuction", (auctionId) => {
    currentAuctionId = parseInt(new URLSearchParams(window.location.search).get("id"), 10);
    if (currentAuctionId == auctionId) {
        window.location.href = window.location.href;
    }
});

connection.on("AuctionCreated", () => {
    var updatedAuctions = document.getElementById("updatedAuctions");
    updatedAuctions.text = "There was an update to the auctions. Please reload the page to see the changes.";
});