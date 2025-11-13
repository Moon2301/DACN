using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DACN.Migrations
{
    /// <inheritdoc />
    public partial class init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Genres",
                columns: table => new
                {
                    GenreId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Genres", x => x.GenreId);
                });

            migrationBuilder.CreateTable(
                name: "Tags",
                columns: table => new
                {
                    TagId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tags", x => x.TagId);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Username = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DateOfBirth = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Bio = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Role = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AvatarUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Money = table.Column<int>(type: "int", nullable: false),
                    ActivePoint = table.Column<int>(type: "int", nullable: false),
                    Ticket = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    IsBanned = table.Column<bool>(type: "bit", nullable: false),
                    BannedUntil = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.UserId);
                });

            migrationBuilder.CreateTable(
                name: "ActivePointTransactions",
                columns: table => new
                {
                    ActivePointTransactionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<int>(type: "int", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivePointTransactions", x => x.ActivePointTransactionId);
                    table.ForeignKey(
                        name: "FK_ActivePointTransactions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DailyRewards",
                columns: table => new
                {
                    RewardId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    RewardDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ActivePointAmount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DailyRewards", x => x.RewardId);
                    table.ForeignKey(
                        name: "FK_DailyRewards_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MoneyTopUpBills",
                columns: table => new
                {
                    MoneyTopUpBillId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MoneyTopUpBills", x => x.MoneyTopUpBillId);
                    table.ForeignKey(
                        name: "FK_MoneyTopUpBills_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    NotificationId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Message = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsRead = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.NotificationId);
                    table.ForeignKey(
                        name: "FK_Notifications_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Reports",
                columns: table => new
                {
                    ReportId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    TargetType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TargetId = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsResolved = table.Column<bool>(type: "bit", nullable: false),
                    ResolvedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReportedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reports", x => x.ReportId);
                    table.ForeignKey(
                        name: "FK_Reports_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Stories",
                columns: table => new
                {
                    StoryId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Author = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CoverImage = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    GenreId = table.Column<int>(type: "int", nullable: false),
                    UploadedByUserId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    TotalReads = table.Column<int>(type: "int", nullable: false),
                    TotalChapters = table.Column<int>(type: "int", nullable: false),
                    TotalRatings = table.Column<int>(type: "int", nullable: false),
                    AverageRating = table.Column<decimal>(type: "decimal(3,2)", precision: 3, scale: 2, nullable: false),
                    TotalComments = table.Column<int>(type: "int", nullable: false),
                    TotalTicketsEarned = table.Column<int>(type: "int", nullable: false),
                    TotalBookmarks = table.Column<int>(type: "int", nullable: false),
                    TotalFollowers = table.Column<int>(type: "int", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Stories", x => x.StoryId);
                    table.ForeignKey(
                        name: "FK_Stories_Genres_GenreId",
                        column: x => x.GenreId,
                        principalTable: "Genres",
                        principalColumn: "GenreId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Stories_Users_UploadedByUserId",
                        column: x => x.UploadedByUserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Chapters",
                columns: table => new
                {
                    ChapterId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StoryId = table.Column<int>(type: "int", nullable: false),
                    ChapterNumber = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    IsVip = table.Column<bool>(type: "bit", nullable: false),
                    VipUnlockAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UnlockPriceMoney = table.Column<int>(type: "int", nullable: false),
                    UnlockPriceActivePoint = table.Column<int>(type: "int", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Chapters", x => x.ChapterId);
                    table.ForeignKey(
                        name: "FK_Chapters_Stories_StoryId",
                        column: x => x.StoryId,
                        principalTable: "Stories",
                        principalColumn: "StoryId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Ratings",
                columns: table => new
                {
                    RatingId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    StoryId = table.Column<int>(type: "int", nullable: false),
                    Score = table.Column<decimal>(type: "decimal(3,2)", precision: 3, scale: 2, nullable: false),
                    Review = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ratings", x => x.RatingId);
                    table.ForeignKey(
                        name: "FK_Ratings_Stories_StoryId",
                        column: x => x.StoryId,
                        principalTable: "Stories",
                        principalColumn: "StoryId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Ratings_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StoryRankings",
                columns: table => new
                {
                    StoryRankingId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    GenreId = table.Column<int>(type: "int", nullable: true),
                    Rank = table.Column<int>(type: "int", nullable: false),
                    StoryId = table.Column<int>(type: "int", nullable: false),
                    StoryTitle = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Author = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CoverImage = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Score = table.Column<int>(type: "int", nullable: false),
                    GeneratedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StoryRankings", x => x.StoryRankingId);
                    table.ForeignKey(
                        name: "FK_StoryRankings_Stories_StoryId",
                        column: x => x.StoryId,
                        principalTable: "Stories",
                        principalColumn: "StoryId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StoryTags",
                columns: table => new
                {
                    StoryTagId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StoryId = table.Column<int>(type: "int", nullable: false),
                    TagId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StoryTags", x => x.StoryTagId);
                    table.ForeignKey(
                        name: "FK_StoryTags_Stories_StoryId",
                        column: x => x.StoryId,
                        principalTable: "Stories",
                        principalColumn: "StoryId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StoryTags_Tags_TagId",
                        column: x => x.TagId,
                        principalTable: "Tags",
                        principalColumn: "TagId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TicketTransactions",
                columns: table => new
                {
                    TicketTransactionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    StoryId = table.Column<int>(type: "int", nullable: true),
                    Amount = table.Column<int>(type: "int", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TicketTransactions", x => x.TicketTransactionId);
                    table.ForeignKey(
                        name: "FK_TicketTransactions_Stories_StoryId",
                        column: x => x.StoryId,
                        principalTable: "Stories",
                        principalColumn: "StoryId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_TicketTransactions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Bookmarks",
                columns: table => new
                {
                    BookmarkId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    StoryId = table.Column<int>(type: "int", nullable: false),
                    ChapterId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bookmarks", x => x.BookmarkId);
                    table.ForeignKey(
                        name: "FK_Bookmarks_Chapters_ChapterId",
                        column: x => x.ChapterId,
                        principalTable: "Chapters",
                        principalColumn: "ChapterId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Bookmarks_Stories_StoryId",
                        column: x => x.StoryId,
                        principalTable: "Stories",
                        principalColumn: "StoryId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Bookmarks_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ChapterReadedByUsers",
                columns: table => new
                {
                    ReadId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    ChapterId = table.Column<int>(type: "int", nullable: false),
                    ReadAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChapterReadedByUsers", x => x.ReadId);
                    table.ForeignKey(
                        name: "FK_ChapterReadedByUsers_Chapters_ChapterId",
                        column: x => x.ChapterId,
                        principalTable: "Chapters",
                        principalColumn: "ChapterId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChapterReadedByUsers_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Comments",
                columns: table => new
                {
                    CommentId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    StoryId = table.Column<int>(type: "int", nullable: true),
                    ChapterId = table.Column<int>(type: "int", nullable: true),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    ParentCommentId = table.Column<int>(type: "int", nullable: true),
                    RepliesCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Comments", x => x.CommentId);
                    table.ForeignKey(
                        name: "FK_Comments_Chapters_ChapterId",
                        column: x => x.ChapterId,
                        principalTable: "Chapters",
                        principalColumn: "ChapterId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Comments_Comments_ParentCommentId",
                        column: x => x.ParentCommentId,
                        principalTable: "Comments",
                        principalColumn: "CommentId");
                    table.ForeignKey(
                        name: "FK_Comments_Stories_StoryId",
                        column: x => x.StoryId,
                        principalTable: "Stories",
                        principalColumn: "StoryId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Comments_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FollowedStoryByUsers",
                columns: table => new
                {
                    FollowId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    StoryId = table.Column<int>(type: "int", nullable: false),
                    CurrentChapterId = table.Column<int>(type: "int", nullable: true),
                    FollowedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FollowedStoryByUsers", x => x.FollowId);
                    table.ForeignKey(
                        name: "FK_FollowedStoryByUsers_Chapters_CurrentChapterId",
                        column: x => x.CurrentChapterId,
                        principalTable: "Chapters",
                        principalColumn: "ChapterId");
                    table.ForeignKey(
                        name: "FK_FollowedStoryByUsers_Stories_StoryId",
                        column: x => x.StoryId,
                        principalTable: "Stories",
                        principalColumn: "StoryId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FollowedStoryByUsers_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MoneyTransactions",
                columns: table => new
                {
                    MoneyTransactionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    ChapterId = table.Column<int>(type: "int", nullable: true),
                    Amount = table.Column<int>(type: "int", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MoneyTransactions", x => x.MoneyTransactionId);
                    table.ForeignKey(
                        name: "FK_MoneyTransactions_Chapters_ChapterId",
                        column: x => x.ChapterId,
                        principalTable: "Chapters",
                        principalColumn: "ChapterId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_MoneyTransactions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UnlockedChapters",
                columns: table => new
                {
                    UnlockId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    ChapterId = table.Column<int>(type: "int", nullable: false),
                    UsedMoney = table.Column<int>(type: "int", nullable: false),
                    UsedActivePoint = table.Column<int>(type: "int", nullable: false),
                    UnlockedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UnlockedChapters", x => x.UnlockId);
                    table.ForeignKey(
                        name: "FK_UnlockedChapters_Chapters_ChapterId",
                        column: x => x.ChapterId,
                        principalTable: "Chapters",
                        principalColumn: "ChapterId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UnlockedChapters_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "UserId", "ActivePoint", "AvatarUrl", "BannedUntil", "Bio", "CreatedAt", "DateOfBirth", "Email", "IsBanned", "IsDeleted", "Money", "PasswordHash", "PhoneNumber", "Role", "Ticket", "Username" },
                values: new object[] { 1, 999999, "/images/defaulAvatar.png", null, "Default admin account", new DateTime(2025, 11, 13, 7, 20, 48, 947, DateTimeKind.Utc).AddTicks(1710), null, "admin@example.com", false, false, 999999, "$2a$11$FOf9K14NDrvEPRaywww6EuChaYDmzuHTtzgIKzILo0bdKzY7KHoMq", null, "Admin", 999999, "admin" });

            migrationBuilder.CreateIndex(
                name: "IX_ActivePointTransactions_UserId",
                table: "ActivePointTransactions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Bookmarks_ChapterId",
                table: "Bookmarks",
                column: "ChapterId");

            migrationBuilder.CreateIndex(
                name: "IX_Bookmarks_StoryId",
                table: "Bookmarks",
                column: "StoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Bookmarks_UserId",
                table: "Bookmarks",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ChapterReadedByUsers_ChapterId",
                table: "ChapterReadedByUsers",
                column: "ChapterId");

            migrationBuilder.CreateIndex(
                name: "IX_ChapterReadedByUsers_UserId",
                table: "ChapterReadedByUsers",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Chapters_StoryId",
                table: "Chapters",
                column: "StoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Comments_ChapterId",
                table: "Comments",
                column: "ChapterId");

            migrationBuilder.CreateIndex(
                name: "IX_Comments_ParentCommentId",
                table: "Comments",
                column: "ParentCommentId");

            migrationBuilder.CreateIndex(
                name: "IX_Comments_StoryId",
                table: "Comments",
                column: "StoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Comments_UserId",
                table: "Comments",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_DailyRewards_UserId",
                table: "DailyRewards",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_FollowedStoryByUsers_CurrentChapterId",
                table: "FollowedStoryByUsers",
                column: "CurrentChapterId");

            migrationBuilder.CreateIndex(
                name: "IX_FollowedStoryByUsers_StoryId",
                table: "FollowedStoryByUsers",
                column: "StoryId");

            migrationBuilder.CreateIndex(
                name: "IX_FollowedStoryByUsers_UserId",
                table: "FollowedStoryByUsers",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_MoneyTopUpBills_UserId",
                table: "MoneyTopUpBills",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_MoneyTransactions_ChapterId",
                table: "MoneyTransactions",
                column: "ChapterId");

            migrationBuilder.CreateIndex(
                name: "IX_MoneyTransactions_UserId",
                table: "MoneyTransactions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId",
                table: "Notifications",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Ratings_StoryId",
                table: "Ratings",
                column: "StoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Ratings_UserId",
                table: "Ratings",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Reports_UserId",
                table: "Reports",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Stories_GenreId",
                table: "Stories",
                column: "GenreId");

            migrationBuilder.CreateIndex(
                name: "IX_Stories_UploadedByUserId",
                table: "Stories",
                column: "UploadedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_StoryRankings_StoryId",
                table: "StoryRankings",
                column: "StoryId");

            migrationBuilder.CreateIndex(
                name: "IX_StoryTags_StoryId",
                table: "StoryTags",
                column: "StoryId");

            migrationBuilder.CreateIndex(
                name: "IX_StoryTags_TagId",
                table: "StoryTags",
                column: "TagId");

            migrationBuilder.CreateIndex(
                name: "IX_TicketTransactions_StoryId",
                table: "TicketTransactions",
                column: "StoryId");

            migrationBuilder.CreateIndex(
                name: "IX_TicketTransactions_UserId",
                table: "TicketTransactions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UnlockedChapters_ChapterId",
                table: "UnlockedChapters",
                column: "ChapterId");

            migrationBuilder.CreateIndex(
                name: "IX_UnlockedChapters_UserId",
                table: "UnlockedChapters",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ActivePointTransactions");

            migrationBuilder.DropTable(
                name: "Bookmarks");

            migrationBuilder.DropTable(
                name: "ChapterReadedByUsers");

            migrationBuilder.DropTable(
                name: "Comments");

            migrationBuilder.DropTable(
                name: "DailyRewards");

            migrationBuilder.DropTable(
                name: "FollowedStoryByUsers");

            migrationBuilder.DropTable(
                name: "MoneyTopUpBills");

            migrationBuilder.DropTable(
                name: "MoneyTransactions");

            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropTable(
                name: "Ratings");

            migrationBuilder.DropTable(
                name: "Reports");

            migrationBuilder.DropTable(
                name: "StoryRankings");

            migrationBuilder.DropTable(
                name: "StoryTags");

            migrationBuilder.DropTable(
                name: "TicketTransactions");

            migrationBuilder.DropTable(
                name: "UnlockedChapters");

            migrationBuilder.DropTable(
                name: "Tags");

            migrationBuilder.DropTable(
                name: "Chapters");

            migrationBuilder.DropTable(
                name: "Stories");

            migrationBuilder.DropTable(
                name: "Genres");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
