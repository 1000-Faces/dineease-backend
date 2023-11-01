﻿using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.OpenApi;
using webapi.Models;
using Microsoft.AspNetCore.Mvc;
using webapi.Services;
using Firebase.Auth;
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using webapi.DataModels;
using Microsoft.AspNetCore.Http;

namespace webapi.Endpoints;

public static class UserEndpoints
{
    public static void MapUserEndpoints(this IEndpointRouteBuilder routes)
    {
        FirebaseAuthProvider auth;

        var group = routes.MapGroup("/api/user").WithTags(nameof(webapi.Models.User));

        group.MapGet("/", async (MainDatabaseContext db) =>
        {
            return await db.User.ToListAsync();
        })
        .WithName("GetAllUsers")
        .WithOpenApi();

        group.MapGet("/get", async Task<Results<Ok<webapi.Models.User>, NotFound, BadRequest<string>>> (Guid? id, string? email, MainDatabaseContext db) =>
        {
            if (id.HasValue)
            {
                // If "id" parameter is provided and valid, get user by id
                return await db.User.AsNoTracking()
                    .FirstOrDefaultAsync(model => model.Id == id.Value)
                    is webapi.Models.User model
                        ? TypedResults.Ok(model)
                        : TypedResults.NotFound();
            }
            else if (!string.IsNullOrEmpty(email))
            {
                // If "email" parameter is provided, get user by email
                return await db.User.AsNoTracking()
                    .FirstOrDefaultAsync(model => model.Email == email)
                    is webapi.Models.User model
                        ? TypedResults.Ok(model)
                        : TypedResults.NotFound();
            }
            else
            {
                // Neither "id" nor "email" parameter is provided
                // Return a bad request response or handle it based on your specific use case
                return TypedResults.BadRequest("Invalid request. Either 'id' or 'email' parameter is required.");
            }
        })
        .WithName("GetUserByEmailOrId")
        .WithOpenApi();

        group.MapPut("/{id}", async Task<Results<Ok, NotFound>> (Guid id, webapi.Models.User user, MainDatabaseContext db) =>
        {
            var affected = await db.User
                .Where(model => model.Id == id)
                .ExecuteUpdateAsync(setters => setters
                  .SetProperty(m => m.Id, user.Id)
                  .SetProperty(m => m.Username, user.Username)
                  .SetProperty(m => m.Name, user.Name)
                  .SetProperty(m => m.Address, user.Address)
                  .SetProperty(m => m.Phone, user.Phone)
                  .SetProperty(m => m.Email, user.Email)
                  .SetProperty(m => m.UserImage, user.UserImage)
                  .SetProperty(m => m.UserImageType, user.UserImageType)
                  .SetProperty(m => m.CreatedDate, user.CreatedDate)
                );

            return affected == 1 ? TypedResults.Ok() : TypedResults.NotFound();
        })
        .WithName("UpdateUser")
        .WithOpenApi();

        group.MapPost("/", async Task<Results<Created<Guid>, BadRequest<string>, Ok<string>>> (webapi.Models.User user, MainDatabaseContext db) =>
        {
            // Check if user already exists
            if (await db.User.AnyAsync(model => model.Email == user.Email))
            {
                return TypedResults.BadRequest("User already exists.");
            }

            // Check if user has authentication data
            if (user.Authentication == null)
            {
                return TypedResults.BadRequest("User authentication data is required.");
            }

            // Register IHttpContextAccessor in ConfigureServices
            var serviceProvider = routes.ServiceProvider;
            var httpContextAccessor = serviceProvider.GetRequiredService<IHttpContextAccessor>();

            // Firebase Web api tocken
            auth = new FirebaseAuthProvider(new FirebaseConfig("AIzaSyBELGN_oHdUVGX_38-thPz6Ca6JTnmjwm0"));
            //create the user
            await auth.CreateUserWithEmailAndPasswordAsync(user.Email, user.Authentication.Password);
            //log in the new user
            var fbAuthLink = await auth.SignInWithEmailAndPasswordAsync(user.Email, user.Authentication.Password);
            string token = fbAuthLink.FirebaseToken;
            //saving the token in a session variable
            if (token != null)
            {
                var context = httpContextAccessor.HttpContext;
                context?.Session.SetString("_UserToken", token);

                //return TypedResults.Ok(token);
            }

            // create a new guid for the user
            user.Id = Guid.NewGuid();
            // secure the password
            var hashedPassword = PasswordHasher.Hash(user.Authentication.Password);
            user.Authentication.Password = hashedPassword.Item1;
            user.Authentication.Salt = hashedPassword.Item2;
            // set created date
            user.CreatedDate = DateTime.Today;
            user.Authentication.LastUpdated = DateTime.Now;

            db.User.Add(user);
            await db.SaveChangesAsync();
            return TypedResults.Created($"/api/User/", user.Id);
        })
        .WithName("CreateUser")
        .WithOpenApi();

        // Authenticate user by email and password
        group.MapPost("/androidLogin", async Task<Results<Accepted<Guid>, Ok<string>, UnauthorizedHttpResult>> (LoginData data, MainDatabaseContext db) =>
        {
            // Register IHttpContextAccessor in ConfigureServices
            var serviceProvider = routes.ServiceProvider;
            var httpContextAccessor = serviceProvider.GetRequiredService<IHttpContextAccessor>();

            // Firebase Web api tocken
            auth = new FirebaseAuthProvider(new FirebaseConfig("AIzaSyBELGN_oHdUVGX_38-thPz6Ca6JTnmjwm0"));

            //log in an existing user
            var fbAuthLink = await auth.SignInWithEmailAndPasswordAsync(data.Email, data.Password);
            string token = fbAuthLink.FirebaseToken;
            //save the token to a session variable
            if (token != null)
            {
                var context = httpContextAccessor.HttpContext;
                context?.Session.SetString("_UserToken", token);

                return TypedResults.Ok(token);
            }
            else
            {
                return TypedResults.Unauthorized();
            }
            
        })
        .WithName("AuthenticateAppUser")
        .WithOpenApi();

        group.MapDelete("/{id}", async Task<Results<Ok, NotFound>> (Guid id, MainDatabaseContext db) =>
        {
            var affected = await db.User
                .Where(model => model.Id == id)
                .ExecuteDeleteAsync();

            return affected == 1 ? TypedResults.Ok() : TypedResults.NotFound();
        })
        .WithName("DeleteUser")
        .WithOpenApi();
    }
}
