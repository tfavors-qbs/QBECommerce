window.localStorage = {
    getItem: function (key) {
        return window.localStorage.getItem(key);
    },
    setItem: function (key, value) {
        window.localStorage.setItem(key, value);
    },
    removeItem: function (key) {
        window.localStorage.removeItem(key);
    }
};

window.sessionStorage = {
    getItem: function (key) {
        return window.sessionStorage.getItem(key);
    },
    setItem: function (key, value) {
        window.sessionStorage.setItem(key, value);
    },
    removeItem: function (key) {
        window.sessionStorage.removeItem(key);
    }
};