﻿namespace Gateway.Services.Services
{
    public interface IJwtAuthenticationManager
    {
        string Authenticate(string username, string password);
    }
}
