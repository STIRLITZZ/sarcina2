﻿using BlazorApp11.Shared.Models;
using Blazored.LocalStorage;
using Newtonsoft.Json;
using System.Net.Http.Json;

namespace BlazorApp11.Client.ClientServices
{
    public class ClientService : IClientService
    {

        private readonly ILocalStorageService localStorageService;
        private readonly HttpClient httpClient;
        public ClientService(ILocalStorageService localStorageService, HttpClient httpClient) {
            this.localStorageService = localStorageService;
            this.httpClient = httpClient;
        }

        //Public Methods with no Token needed
        public async Task<UserSession> LoginAsync(Login model)
        {
            var result = await httpClient.PostAsJsonAsync("https://localhost:7285/api/Authentication/login", model);
            var response = await result.Content.ReadFromJsonAsync<UserSession>();
            return response!;
        }

        public async Task<object> RegisterAccountAsync(RegisterModel model)
        {
            var result = await httpClient.PostAsJsonAsync("https://localhost:7285/api/Authentication/Register", model);
            var response = await result.Content.ReadAsStringAsync();
            return response;
        }

        // Protected Methods which need Token.
        public async Task<int> GetUserCount()
        {
            var securedClient = await SecuredClient();
            var result = await securedClient.GetAsync("https://localhost:7285/api/authentication/total-users");
            if (result.StatusCode != System.Net.HttpStatusCode.Unauthorized)
                return await result.Content.ReadFromJsonAsync<int>();

            await GetNewToken();
            int response = await GetUserCount();
            return response;
        }

        // General & Frequent Call-up Methods
        private async Task<bool> GetNewToken()
        {
            var token = await localStorageService.GetItemAsync<string>("token");
            if (string.IsNullOrEmpty(token)) return false;

            var getNetTokenAndRefreshToken = await httpClient.PostAsJsonAsync("https://localhost:7285/api/authentication/GetNewToken", DeSerializedUserSession(token));
            var response = await getNetTokenAndRefreshToken.Content.ReadFromJsonAsync<UserSession>();

            if (response is null) return false;

            var serializedUserSeeion = SerializedUserSession(response!);
            await localStorageService.RemoveItemAsync("token");
            await localStorageService.SetItemAsync("token", serializedUserSeeion);
            await SecuredClient();
            return true;
        }

        private async Task<HttpClient> SecuredClient()
        {
            var client = new HttpClient();
            var token = await localStorageService.GetItemAsync<string>("token");
            var userSession = DeSerializedUserSession(token);
            if (userSession is null) return client;

            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", userSession.Token);
            return client;
        }

        private static UserSession DeSerializedUserSession(string SerialisedString)
        {
            return JsonConvert.DeserializeObject<UserSession>(SerialisedString)!;
        }

        private static string SerializedUserSession(UserSession userSession)
        {
            return JsonConvert.SerializeObject(userSession);
        }

        public Task<string> GetMyInfo(string email)
        {
            throw new NotImplementedException();
        }
    }
}
