using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QBExternalWebLibrary.Services {
    public class LocalStorageService {
        private readonly IJSRuntime _jsRuntime;

        public LocalStorageService(IJSRuntime jsRuntime) {
            _jsRuntime = jsRuntime;
        }

        public async Task SetItemAsync(string key, string value) {
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", key, value);
        }

        public async Task<string> GeItemAsync(string key) {
            return await _jsRuntime.InvokeAsync<string>("localStorage.getItem", key);
        }

        public async Task RemoveItemAsync(string key) {
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", key);
        }
    }
}
