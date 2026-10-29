using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Models;

namespace RaceDay.Api.Data;

public static class DbSeeder
{
    public static async Task InitialiseAndSeedAsync(RaceDayDbContext db, ILogger logger)
    {
        try
        {
            // Ensure database and base schema exist
            await db.Database.EnsureCreatedAsync();

            // Idempotently ensure Part 3 schema additions exist if upgrading from a Part 2 database
            const string upgradeSql = """
                IF COL_LENGTH('dbo.Events', 'Distance') IS NULL
                BEGIN
                    ALTER TABLE dbo.Events ADD Distance decimal(6,2) NOT NULL CONSTRAINT DF_Events_Distance DEFAULT (0);
                END;

                IF COL_LENGTH('dbo.Events', 'BannerImageUrl') IS NULL
                BEGIN
                    ALTER TABLE dbo.Events ADD BannerImageUrl nvarchar(500) NULL;
                END;

                IF COL_LENGTH('dbo.Users', 'ProfilePictureUrl') IS NULL
                BEGIN
                    ALTER TABLE dbo.Users ADD ProfilePictureUrl nvarchar(500) NULL;
                END;

                IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_Enrolments_Status')
                BEGIN
                    ALTER TABLE dbo.Enrolments DROP CONSTRAINT CK_Enrolments_Status;
                END;
                ALTER TABLE dbo.Enrolments WITH NOCHECK
                    ADD CONSTRAINT CK_Enrolments_Status CHECK (Status IN ('Pending', 'Registered', 'Confirmed', 'Withdrawn', 'DNS'));
                """;

            await db.Database.ExecuteSqlRawAsync(upgradeSql);

            // Ensure Roles exist
            if (!await db.Roles.AnyAsync())
            {
                await db.Database.ExecuteSqlRawAsync("""
                    SET IDENTITY_INSERT dbo.Roles ON;
                    INSERT INTO dbo.Roles (RoleId, RoleName) VALUES (1, 'Organiser'), (2, 'Participant');
                    SET IDENTITY_INSERT dbo.Roles OFF;
                    """);
            }

            // Seed demo users and events if none exist
            if (!await db.Users.AnyAsync())
            {
                var organiserRole = await db.Roles.FirstAsync(r => r.RoleName == "Organiser");
                var participantRole = await db.Roles.FirstAsync(r => r.RoleName == "Participant");

                var organiser = new User
                {
                    FirstName = "Sipho",
                    LastName = "Mokoena",
                    Email = "organiser@raceday.co.za",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("RaceDay@2026"),
                    Gender = "Male",
                    PhoneNumber = "+27 82 555 0101",
                    City = "Cape Town",
                    Province = "Western Cape",
                    DateOfBirth = new DateOnly(1985, 4, 14),
                    CreatedAt = DateTime.UtcNow
                };

                var participant = new User
                {
                    FirstName = "Lerato",
                    LastName = "Ndlovu",
                    Email = "runner@raceday.co.za",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("RaceDay@2026"),
                    Gender = "Female",
                    PhoneNumber = "+27 83 555 0199",
                    City = "Johannesburg",
                    Province = "Gauteng",
                    DateOfBirth = new DateOnly(1996, 8, 22),
                    CreatedAt = DateTime.UtcNow
                };

                var participant2 = new User
                {
                    FirstName = "Liam",
                    LastName = "van der Merwe",
                    Email = "liam@raceday.co.za",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("RaceDay@2026"),
                    Gender = "Male",
                    PhoneNumber = "+27 84 555 0244",
                    City = "Durban",
                    Province = "KwaZulu-Natal",
                    DateOfBirth = new DateOnly(1992, 11, 3),
                    CreatedAt = DateTime.UtcNow
                };

                var participant3 = new User
                {
                    FirstName = "Amahle",
                    LastName = "Dlamini",
                    Email = "amahle@raceday.co.za",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("RaceDay@2026"),
                    Gender = "Female",
                    PhoneNumber = "+27 72 555 0311",
                    City = "Pretoria",
                    Province = "Gauteng",
                    DateOfBirth = new DateOnly(1999, 2, 17),
                    CreatedAt = DateTime.UtcNow
                };

                db.Users.AddRange(organiser, participant, participant2, participant3);
                await db.SaveChangesAsync();

                db.UserRoles.AddRange(
                    new UserRole { UserId = organiser.UserId, RoleId = organiserRole.RoleId },
                    new UserRole { UserId = participant.UserId, RoleId = participantRole.RoleId },
                    new UserRole { UserId = participant2.UserId, RoleId = participantRole.RoleId },
                    new UserRole { UserId = participant3.UserId, RoleId = participantRole.RoleId }
                );
                await db.SaveChangesAsync();

                var today = DateOnly.FromDateTime(DateTime.UtcNow);

                var ev1 = new Event
                {
                    OrganiserId = organiser.UserId,
                    Name = "Cape Town Atlantic Marathon",
                    Description = "Experience the breathtaking Atlantic Seaboard road marathon starting at Green Point Stadium and winding past Sea Point, Clifton, and Table Mountain vistas.",
                    EventDate = today.AddDays(21),
                    StartTime = new TimeOnly(6, 0),
                    Location = "Green Point Athletics Stadium",
                    City = "Cape Town",
                    Province = "Western Cape",
                    Distance = 42.20m,
                    EventType = "Running",
                    Status = "Published",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                var ev2 = new Event
                {
                    OrganiserId = organiser.UserId,
                    Name = "Joburg Skyline Night Run",
                    Description = "A fast, electric 10km and 5km city night run through Rosebank and Sandton under stadium floodlights with live DJs at the finish village.",
                    EventDate = today.AddDays(35),
                    StartTime = new TimeOnly(18, 30),
                    Location = "Mushroom Farm Park, Sandton",
                    City = "Johannesburg",
                    Province = "Gauteng",
                    Distance = 10.00m,
                    EventType = "Running",
                    Status = "Published",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                var ev3 = new Event
                {
                    OrganiserId = organiser.UserId,
                    Name = "Durban Coastal Velocity Cycle Challenge",
                    Description = "Flat, high-speed coastal road cycling race along the Golden Mile and M4 highway with full road closure and electronic chip timing.",
                    EventDate = today.AddDays(50),
                    StartTime = new TimeOnly(5, 30),
                    Location = "Moses Mabhida Promenade",
                    City = "Durban",
                    Province = "KwaZulu-Natal",
                    Distance = 95.00m,
                    EventType = "Cycling",
                    Status = "Published",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                var ev4 = new Event
                {
                    OrganiserId = organiser.UserId,
                    Name = "Pretoria Jacaranda Sunset Walk",
                    Description = "Scenic community heritage walk and power-striding challenge beneath Pretoria's iconic purple Jacaranda canopy.",
                    EventDate = today.AddDays(64),
                    StartTime = new TimeOnly(7, 0),
                    Location = "Union Buildings Lawns",
                    City = "Pretoria",
                    Province = "Gauteng",
                    Distance = 8.00m,
                    EventType = "Walking",
                    Status = "Published",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                var evCompleted = new Event
                {
                    OrganiserId = organiser.UserId,
                    Name = "Gauteng Half-Marathon Championship",
                    Description = "Official provincial half-marathon and 10km road race with certified timing and qualifiers.",
                    EventDate = today.AddDays(-14),
                    StartTime = new TimeOnly(6, 0),
                    Location = "Wanderers Club Precinct",
                    City = "Johannesburg",
                    Province = "Gauteng",
                    Distance = 21.10m,
                    EventType = "Running",
                    Status = "Completed",
                    CreatedAt = DateTime.UtcNow.AddDays(-45),
                    UpdatedAt = DateTime.UtcNow.AddDays(-13)
                };

                db.Events.AddRange(ev1, ev2, ev3, ev4, evCompleted);
                await db.SaveChangesAsync();

                var cat1Full = new Category { EventId = ev1.EventId, Name = "42.2km Full Marathon (Open 18+)", Distance = 42.20m, EntryFee = 450.00m, MaxParticipants = 500 };
                var cat1Half = new Category { EventId = ev1.EventId, Name = "21.1km Half Marathon", Distance = 21.10m, EntryFee = 280.00m, MaxParticipants = 800 };
                var cat1Ten = new Category { EventId = ev1.EventId, Name = "10km Atlantic Dash", Distance = 10.00m, EntryFee = 160.00m, MaxParticipants = 1000 };

                var cat2Ten = new Category { EventId = ev2.EventId, Name = "10km Night Challenge", Distance = 10.00m, EntryFee = 180.00m, MaxParticipants = 600 };
                var cat2Five = new Category { EventId = ev2.EventId, Name = "5km Neon Fun Run (Under 18 & Open)", Distance = 5.00m, EntryFee = 110.00m, MaxParticipants = 600 };

                var cat3Long = new Category { EventId = ev3.EventId, Name = "95km Elite & Open Road Classic", Distance = 95.00m, EntryFee = 520.00m, MaxParticipants = 400 };
                var cat3Short = new Category { EventId = ev3.EventId, Name = "45km Coastal Sprint", Distance = 45.00m, EntryFee = 320.00m, MaxParticipants = 500 };

                var cat4Walk = new Category { EventId = ev4.EventId, Name = "8km Heritage Stride", Distance = 8.00m, EntryFee = 95.00m, MaxParticipants = 400 };

                var catCompHalf = new Category { EventId = evCompleted.EventId, Name = "21.1km Championship Half", Distance = 21.10m, EntryFee = 260.00m, MaxParticipants = 500 };
                var catCompTen = new Category { EventId = evCompleted.EventId, Name = "10km Speed Series", Distance = 10.00m, EntryFee = 150.00m, MaxParticipants = 500 };

                db.Categories.AddRange(cat1Full, cat1Half, cat1Ten, cat2Ten, cat2Five, cat3Long, cat3Short, cat4Walk, catCompHalf, catCompTen);
                await db.SaveChangesAsync();

                var enr1 = new Enrolment
                {
                    ParticipantId = participant.UserId,
                    CategoryId = cat1Half.CategoryId,
                    BibNumber = "CT-0101",
                    Status = "Confirmed",
                    EnrolmentDate = DateTime.UtcNow.AddDays(-5)
                };

                var enr2 = new Enrolment
                {
                    ParticipantId = participant.UserId,
                    CategoryId = cat2Ten.CategoryId,
                    BibNumber = "JS-0101",
                    Status = "Pending",
                    EnrolmentDate = DateTime.UtcNow.AddDays(-2)
                };

                var enr3 = new Enrolment
                {
                    ParticipantId = participant2.UserId,
                    CategoryId = cat1Full.CategoryId,
                    BibNumber = "CT-0102",
                    Status = "Confirmed",
                    EnrolmentDate = DateTime.UtcNow.AddDays(-4)
                };

                var enrComp1 = new Enrolment
                {
                    ParticipantId = participant.UserId,
                    CategoryId = catCompHalf.CategoryId,
                    BibNumber = "GH-0101",
                    Status = "Confirmed",
                    EnrolmentDate = DateTime.UtcNow.AddDays(-30)
                };

                var enrComp2 = new Enrolment
                {
                    ParticipantId = participant2.UserId,
                    CategoryId = catCompHalf.CategoryId,
                    BibNumber = "GH-0102",
                    Status = "Confirmed",
                    EnrolmentDate = DateTime.UtcNow.AddDays(-29)
                };

                var enrComp3 = new Enrolment
                {
                    ParticipantId = participant3.UserId,
                    CategoryId = catCompHalf.CategoryId,
                    BibNumber = "GH-0103",
                    Status = "Confirmed",
                    EnrolmentDate = DateTime.UtcNow.AddDays(-28)
                };

                db.Enrolments.AddRange(enr1, enr2, enr3, enrComp1, enrComp2, enrComp3);
                await db.SaveChangesAsync();

                db.Results.AddRange(
                    new Result
                    {
                        EnrolmentId = enrComp2.EnrolmentId,
                        FinishTime = new TimeOnly(1, 14, 22),
                        Position = 1,
                        Status = "Finished",
                        RecordedAt = DateTime.UtcNow.AddDays(-14)
                    },
                    new Result
                    {
                        EnrolmentId = enrComp1.EnrolmentId,
                        FinishTime = new TimeOnly(1, 38, 45),
                        Position = 2,
                        Status = "Finished",
                        RecordedAt = DateTime.UtcNow.AddDays(-14)
                    },
                    new Result
                    {
                        EnrolmentId = enrComp3.EnrolmentId,
                        FinishTime = new TimeOnly(1, 46, 10),
                        Position = 3,
                        Status = "Finished",
                        RecordedAt = DateTime.UtcNow.AddDays(-14)
                    }
                );
                await db.SaveChangesAsync();

                logger.LogInformation("Seeded RaceDay demo accounts, events, categories, enrolments, and results.");
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Database initialization/seeding encountered an issue: {Message}", ex.Message);
        }
    }
}

