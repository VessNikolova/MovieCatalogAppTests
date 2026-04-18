using System;
using System.Net;
using System.Text.Json;
using NUnit.Framework.Constraints;
using RestSharp;
using RestSharp.Authenticators;
using MovieCatalogTests.DTOs;


namespace MovieCatalogTests
{
    [TestFixture]
    public class Tests
    {
        private RestClient client;
        private static string lastCreatedMovieId;

        private const string BaseUrl = "http://144.91.123.158:5000";


        private const string LoginEmail = "ves123@abv.bg";


        private const string LoginPassword = "123456";

        [OneTimeSetUp]
        public void Setup()
        {
            
            string jwtToken = GetJwtToken(LoginEmail, LoginPassword);

            
            var options = new RestClientOptions(BaseUrl)
            {
                
                Authenticator = new JwtAuthenticator(jwtToken)
            };
            
            this.client = new RestClient(options);
        }

        private string GetJwtToken(string email, string password)
        {
            
            var tempClient = new RestClient(BaseUrl);

            
            var request = new RestRequest("/api/User/Authentication", Method.Post);

            
            request.AddJsonBody(new { email, password });
            
            var response = tempClient.Execute(request);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                
                var content = JsonSerializer.Deserialize<JsonElement>(response.Content);

                
                var token = content.GetProperty("accessToken").GetString();

                if (string.IsNullOrWhiteSpace(token))
                {
                    throw new InvalidOperationException("Token not found in the response.");
                }

                return token;
            }
            else
            {
                throw new InvalidOperationException(
                    $"Failed to authenticate. Status code: {response.StatusCode}, Response: {response.Content}");
            }
        }

        [Order(1)]
        [Test]
        public void CreateNewMovie_WithRequiredFields_ShouldReturnSuccess()
        {
            
            MovieDTO movieData = new MovieDTO
            {
                Title = "Deep Impact",
                Description = "This is test movie.",
            };

            
            var request = new RestRequest("/api/Movie/Create", Method.Post);
            
            request.AddJsonBody(movieData);

            
            var response = this.client.Execute(request);
            
            ApiResponseDTO createResponse = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Expected status code 200 OK.");
            Assert.That(createResponse.Movie, Is.Not.Null);
            Assert.That(createResponse.Movie.Id, Is.Not.Null.And.Not.Empty);
            Assert.That(createResponse.Msg, Is.EqualTo("Movie created successfully!"));

            lastCreatedMovieId = createResponse.Movie.Id;

        }

        [Order(2)]
        [Test]

        public void EditExistingIdea_ShouldReturnSuccess()
        {
            var editedMovie = new MovieDTO
            {
                Title = "Edited Movie",
                Description = "This is a edited movie description."
            };


            var request = new RestRequest("/api/Movie/Edit", Method.Put);

            request.AddQueryParameter("movieId", lastCreatedMovieId);
            request.AddJsonBody(editedMovie);

            var response = this.client.Execute(request);

            var editResponse = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Expected status code 200 OK.");
            Assert.That(editResponse.Msg, Is.EqualTo("Movie edited successfully!"));
        }


        [Order(3)]
        [Test]
        public void GetAllMovies_ShouldReturnSuccess()
        {

            var request = new RestRequest("/api/Catalog/All", Method.Get);
            var response = this.client.Execute(request);

            
            var listMovies = JsonSerializer.Deserialize<List<MovieDTO>>(response.Content);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Expected status code 200 OK.");
            Assert.That(listMovies, Is.Not.Null);
            Assert.That(listMovies, Is.Not.Empty);
            Assert.That(listMovies.Count, Is.GreaterThanOrEqualTo(1));

        }

        [Order(4)]
        [Test]
        public void DeleteExistingMovie_ShouldReturnSuccess()
        {
            RestRequest request = new RestRequest("/api/Movie/Delete", Method.Delete);
            request.AddQueryParameter("movieId", lastCreatedMovieId);
            RestResponse response = client.Execute(request);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            ApiResponseDTO readyResponse = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);

            Assert.That(readyResponse.Msg, Is.EqualTo("Movie deleted successfully!"));
        }

        [Order(5)]
        [Test]
        public void CreateNewMovie_WithoutRequiredFields_ShouldReturnBadRequest()
        {
            var movieData = new MovieDTO
            {
                Title = "",
                Description = ""
            };

            var request = new RestRequest("/api/Movie/Create", Method.Post);
            request.AddJsonBody(movieData);

            var response = this.client.Execute(request);
            var createResponse = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest), "Expected status code 400 Bad Request.");

        }

        [Order(6)]
        [Test]
        public void EditNonExistingMovie_ShouldReturnBadRequest()
        {
            var nonExitingMovieId = "999955999";
            var editedMovie = new MovieDTO
            {
                Title = "Edited Title",
                Description = "Edited Description"
            };

            var request = new RestRequest("/api/Movie/Edit", Method.Put);
            request.AddQueryParameter("movieId", nonExitingMovieId);
            request.AddJsonBody(editedMovie);

            var response = this.client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest), "Expected status code 400 Bad Request.");

            ApiResponseDTO readyResponse = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);
            Assert.That(readyResponse.Msg, Is.EqualTo("Unable to edit the movie! Check the movieId parameter or user verification!"));
        }

        [Order(7)]
        [Test]
        public void DeleteNonExistingMovie_ShouldReturnBadRequest()
        {
            var nonExitingMovieId = "9995599999";

            var request = new RestRequest("/api/Movie/Delete", Method.Delete);
            request.AddQueryParameter("movieId", nonExitingMovieId);
            RestResponse response = client.Execute(request);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));

            ApiResponseDTO readyResponse = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);

            Assert.That(readyResponse.Msg, Is.EqualTo("Unable to delete the movie! Check the movieId parameter or user verification!"));

        }

        [OneTimeTearDown]
        public void TearDown()
        {
            this.client?.Dispose();
        }
    }
}
