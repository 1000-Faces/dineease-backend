﻿using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.OpenApi;
using webapi.Models;
using NuGet.Packaging.Signing;
using System.Text.Json.Serialization;
using System.Text.Json;
using static NuGet.Packaging.PackagingConstants;

namespace webapi.Endpoints;

public static class ReservationEndpoints
{
    public static void MapReservationEndpoints (this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/Reservation").WithTags(nameof(Reservation));

        group.MapGet("/", async (MainDatabaseContext db) =>
        {
            return await db.Reservation.ToListAsync();
        })
        .WithName("GetAllReservations")
        .WithOpenApi();

        group.MapGet("/today", async (MainDatabaseContext db) =>
        {
            DateTime today = DateTime.Now.Date; // Get today's date without the time

            var reservations = await db.Reservation
                .Where(r => r.Departure.HasValue && r.Departure.Value > DateTime.Now && r.Departure.Value.Date == today)
                .ToListAsync();

            return reservations;
        })
        .WithName("GetReservationsForToday")
        .WithOpenApi();

        //group.MapGet("/{id}", async Task<Results<Ok<Reservation>, NotFound>> (string reservationid, MainDatabaseContext db) =>
        group.MapGet("/{id}", async Task<Results<Ok<Reservation>, NotFound>> (Guid reservationid, MainDatabaseContext db) =>
        {
            return await db.Reservation.AsNoTracking()
                .FirstOrDefaultAsync(model => model.ReservationId == reservationid)
                is Reservation model
                    ? TypedResults.Ok(model)
                    : TypedResults.NotFound();
        })
        .WithName("GetReservationById")
        .WithOpenApi();

        //group.MapPut("/{id}", async Task<Results<Ok, NotFound>> (Guid reservationid, Reservation reservation, MainDatabaseContext db) =>
        //{
        //    var affected = await db.Reservation
        //        .Where(model => model.ReservationId == reservationid)
        //        .ExecuteUpdateAsync(setters => setters
        //          //.SetProperty(m => m.ReservationId, reservation.ReservationId)
        //          //.SetProperty(m => m.CustomerId, reservation.CustomerId)
        //          //.SetProperty(m => m.StaffId, reservation.StaffId)
        //          .SetProperty(m => m.TableNo, reservation.TableNo)
        //          .SetProperty(m => m.ReservationDatetime, reservation.ReservationDatetime)
        //          .SetProperty(m => m.Departure, reservation.Departure)
        //          // connot update a time stamp column
        //          //.SetProperty(m => m.ActualDeparture, reservation.ActualDeparture)
        //        );

        //    return affected == 1 ? TypedResults.Ok() : TypedResults.NotFound();
        //})
        //.WithName("UpdateReservation")
        //.WithOpenApi();

        group.MapPut("/{id}", async Task<Results<Ok, NotFound>> (Guid reservationid, Reservation reservation, MainDatabaseContext db) =>
        {
            var reservationToUpdate = await db.Reservation
                .Where(model => model.ReservationId == reservationid)
                .FirstOrDefaultAsync();

            if (reservationToUpdate != null)
            {
                // Update TableNo, ReservationDatetime, and Departure if values are provided
                if (reservation.TableNo != 0)
                {
                    reservationToUpdate.TableNo = reservation.TableNo;
                }
                if (reservation.ReservationDatetime != null)
                {
                    reservationToUpdate.ReservationDatetime = reservation.ReservationDatetime;
                    reservationToUpdate.Departure = reservation.ReservationDatetime.Value.AddHours(2);
                }

                // Save changes to the database
                await db.SaveChangesAsync();

                return TypedResults.Ok();
            }
            else
            {
                return TypedResults.NotFound();
            }
        })
        .WithName("UpdateReservation")
        .WithOpenApi();

        // updating departue time
        group.MapPut("/departure", async Task<Results<Ok, NotFound>> (Guid reservationid, MainDatabaseContext db) =>
        {
            var affected = await db.Reservation
                .Where(model => model.ReservationId == reservationid)
                .ExecuteUpdateAsync(setters => setters
                  .SetProperty(m => m.Departure, DateTime.Now)
                );

            return affected == 1 ? TypedResults.Ok() : TypedResults.NotFound();
        })
        .WithName("Update departure time")
        .WithOpenApi();

        //makeing a reservation
        group.MapPost("/", async (Reservation reservation, MainDatabaseContext db) =>
        {
            reservation.ReservationId = Guid.NewGuid();
            var PtsToUpdate = await db.Customer
                .Where(model => model.UserId == reservation.CustomerId)
                .Select(model => model.LoyalityPts)
                .FirstOrDefaultAsync();

            // Find the customer and update Loyality_pts
            var affected = await db.Customer
                .Where(model => model.UserId == reservation.CustomerId)
                .ExecuteUpdateAsync(setters => setters
                  .SetProperty(m => m.LoyalityPts, PtsToUpdate + 10)
                );

            // for dev perposses, res time is set to current date time 
            // it will be provided on the post req
            if (reservation.ReservationDatetime == null)
            {
                reservation.ReservationDatetime = DateTime.Now.AddHours(1);
                reservation.Departure = reservation.ReservationDatetime.Value.AddHours(2);
            }
            else
            {
                reservation.Departure = reservation.ReservationDatetime.Value.AddHours(2);
            }

            db.Reservation.Add(reservation);
            await db.SaveChangesAsync();
            return TypedResults.Created($"/api/Reservation/{reservation.ReservationId}",reservation);
        })
        .WithName("CreateReservation")
        .WithOpenApi();

        group.MapDelete("/{id}", async Task<Results<Ok, NotFound, BadRequest>> (Guid reservationid, MainDatabaseContext db) =>
        {
            // Retrieve Reservation Information
            var reservationInfo = await db.Reservation
                .Where(r => r.ReservationId == reservationid)
                .Select(r => new
                {
                    r.CustomerId,
                    r.StaffId
                })
                .FirstOrDefaultAsync();

            if (reservationInfo == null)
            {
                return TypedResults.NotFound(); // Return a 404 Not Found response
            }

            // Check Staff Availability (for now)
            // better to check with an arrived column
            if (reservationInfo.StaffId == null)
            {
                return TypedResults.BadRequest(); // Return a 400 Bad Request response
            }

            var PtsToUpdate = await db.Customer
                .Where(model => model.UserId == reservationInfo.CustomerId)
                .Select(model => model.LoyalityPts)
                .FirstOrDefaultAsync();

            // Find the customer and update Loyality_pts
            var affectedCustomer = await db.Customer
                .Where(model => model.UserId == reservationInfo.CustomerId)
                .ExecuteUpdateAsync(setters => setters
                  .SetProperty(m => m.LoyalityPts, PtsToUpdate - 10)
                );

            var affected = await db.Reservation
                .Where(model => model.ReservationId == reservationid)
                .ExecuteDeleteAsync();

            await db.SaveChangesAsync();
            return affected == 1 ? TypedResults.Ok() : TypedResults.NotFound();
        })
        .WithName("DeleteReservation")
        .WithOpenApi();

        // assigning staff
        group.MapPut("/update_staff", async Task<Results<Ok, NotFound>> (Guid reservationid, MainDatabaseContext db) =>
        {
            // Get a random active user_id from the Staff table
            var randomUserId = await db.Staff
                .Where(staff => staff.IsActive == 1)
                .OrderBy(_ => Guid.NewGuid())
                .Select(staff => staff.UserId)
                .FirstOrDefaultAsync();

            if (randomUserId == default(Guid))
            {
                return TypedResults.NotFound(); // No active staff found
            }

            // Update is_active column in the Staff table for the selected user_id
            var staffToUpdate = await db.Staff
                .Where(staff => staff.UserId == randomUserId)
                .FirstOrDefaultAsync();

            if (staffToUpdate == null)
            {
                return TypedResults.NotFound(); // Staff record not found
            }

            staffToUpdate.IsActive = 0;
            db.Staff.Update(staffToUpdate);

            // Update staff_id column in the Reservation table for the given reservationid
            var reservationToUpdate = await db.Reservation
                .Where(reservation => reservation.ReservationId == reservationid)
                .FirstOrDefaultAsync();

            if (reservationToUpdate == null)
            {
                return TypedResults.NotFound(); // Reservation record not found
            }

            reservationToUpdate.StaffId = randomUserId;
            db.Reservation.Update(reservationToUpdate);

            await db.SaveChangesAsync();

            return TypedResults.Ok();
        })
        .WithName("Update staff for reservation")
        .WithOpenApi();

    }
}
