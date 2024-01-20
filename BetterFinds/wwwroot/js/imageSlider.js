document.addEventListener("DOMContentLoaded", function () {
    var images = document.querySelectorAll(".slider-image");
    var currentIndex = 0;

    function showImage(index) {
        images.forEach(function (image, i) {
            image.style.display = i === index ? "block" : "none";
        });

        // Update current index and total images
        var imageCounter = document.getElementById("imageCounter");
        imageCounter.textContent = "Image: " + (index + 1) + "/" + images.length;
    }

    function nextImage() {
        currentIndex = (currentIndex + 1) % images.length;
        showImage(currentIndex);
    }

    function prevImage() {
        currentIndex = (currentIndex - 1 + images.length) % images.length;
        showImage(currentIndex);
    }

    // Initial display
    showImage(currentIndex);

    // Add event listeners for navigation
    document.querySelector(".product-images").addEventListener("click", function (e) {
        if (e.target.classList.contains("slider-image")) {
            nextImage();
        }
    });

    document.querySelector(".product-images").addEventListener("contextmenu", function (e) {
        e.preventDefault();
        prevImage();
    });
});