document.getElementById('searchBar').addEventListener('keypress', function (e) {
    if (e.key === 'Enter') {
        // Get the user-typed query
        var userQuery = document.getElementById('searchBar').value;

        // Redirect to the search URL with the query
        window.location.href = '/search?query=' + encodeURIComponent(userQuery);

        // Prevent the default form submission behavior
        e.preventDefault();
    }
});