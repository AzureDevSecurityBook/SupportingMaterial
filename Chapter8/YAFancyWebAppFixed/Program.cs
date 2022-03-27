#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
YAFancyWebAppFixed.SecurityLogger.Instance.Initialize();
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

//app.UseAuthorization();

app.MapRazorPages();

app.Run();
