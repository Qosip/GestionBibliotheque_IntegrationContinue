using Library.Application;
using Library.Application.Commands;
using Library.Application.Handlers;
using Library.Application.Repositories;
using Library.Domain.Services;
using Library.Infrastructure;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// === Services MVC (UI) ===
builder.Services.AddControllersWithViews();

// === EF Core InMemory ===
builder.Services.AddDbContext<LibraryDbContext>(options =>
    options.UseSqlite("Data Source=library.db"));


// === Repositories (implémentations EF) ===
builder.Services.AddScoped<IUserRepository, EfUserRepository>();
builder.Services.AddScoped<IBookRepository, EfBookRepository>();
builder.Services.AddScoped<ISiteRepository, EfSiteRepository>();
builder.Services.AddScoped<IBookCopyRepository, EfBookCopyRepository>();
builder.Services.AddScoped<ILoanRepository, EfLoanRepository>();

// === Clock + services de domaine ===
builder.Services.AddScoped<IClock, SystemClock>();
builder.Services.AddScoped<BorrowingService>();
builder.Services.AddScoped<PenaltyService>();
builder.Services.AddScoped<ReturnService>();

// === Handlers applicatifs ===
builder.Services.AddScoped<RegisterUserHandler>();
builder.Services.AddScoped<CreateSiteHandler>();
builder.Services.AddScoped<RegisterBookHandler>();
builder.Services.AddScoped<AddBookCopyHandler>();
builder.Services.AddScoped<BorrowBookHandler>();
builder.Services.AddScoped<ReturnBookHandler>();
builder.Services.AddScoped<RequestTransferHandler>();

// === Swagger / OpenAPI pour l'API REST ===
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// === Middleware pipeline ===
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseRouting();

app.UseAuthorization();

// === Routes MVC (UI Razor/MVC) ===
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// === Endpoints REST existants (Minimal API) ===

// Users
app.MapPost("/api/users", (RegisterUserCommand cmd, RegisterUserHandler handler) =>
{
    var result = handler.Handle(cmd);
    return result.Success ? Results.Ok(result) : Results.BadRequest(result);
});

// Sites
app.MapPost("/api/sites", (CreateSiteCommand cmd, CreateSiteHandler handler) =>
{
    var result = handler.Handle(cmd);
    return result.Success ? Results.Ok(result) : Results.BadRequest(result);
});

// Books
app.MapPost("/api/books", (RegisterBookCommand cmd, RegisterBookHandler handler) =>
{
    var result = handler.Handle(cmd);
    return result.Success ? Results.Ok(result) : Results.BadRequest(result);
});

// Book copies
app.MapPost("/api/books/{bookId:guid}/copies", (Guid bookId, AddBookCopyHandler handler, AddBookCopyCommand body) =>
{
    var cmd = new AddBookCopyCommand(bookId, body.SiteId);
    var result = handler.Handle(cmd);
    return result.Success ? Results.Ok(result) : Results.BadRequest(result);
});

// Borrow
app.MapPost("/api/borrow", (BorrowBookCommand cmd, BorrowBookHandler handler) =>
{
    var result = handler.Handle(cmd);
    return result.Success ? Results.Ok(result) : Results.BadRequest(result);
});

// Return
app.MapPost("/api/return", (ReturnBookCommand cmd, ReturnBookHandler handler) =>
{
    var result = handler.Handle(cmd);
    return result.Success ? Results.Ok(result) : Results.BadRequest(result);
});

// Transfer
app.MapPost("/api/transfers", (RequestTransferCommand cmd, RequestTransferHandler handler) =>
{
    var result = handler.Handle(cmd);
    return result.Success ? Results.Ok(result) : Results.BadRequest(result);
});

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<LibraryDbContext>();
    db.Database.EnsureCreated();
}

app.Run();
