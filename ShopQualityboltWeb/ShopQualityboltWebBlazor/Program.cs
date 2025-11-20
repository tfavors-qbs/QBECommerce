// ...existing code...

// configure client for auth interactions with JWT token support
builder.Services.AddHttpClient(
    "Auth",
    opt => opt.BaseAddress = new Uri(builder.Configuration["BackendUrl"] ?? apiAddress))
    .AddHttpMessageHandler<CookieHandler>()
    .AddHttpMessageHandler(sp => 
    {
        var logger = sp.GetService<ILogger<QBExternalWebLibrary.Services.Authentication.JwtTokenHandler>>();
        return new QBExternalWebLibrary.Services.Authentication.JwtTokenHandler(getToken, setToken, logger);
    });

// ...existing code...
