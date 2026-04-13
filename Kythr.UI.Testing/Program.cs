using Microsoft.Extensions.Configuration;

var builder = WebApplication.CreateBuilder(args);

//builder.Services
//    .AddServiceMonitoring(builder.Configuration)
//    .WithServiceMonitoringUi(builder.Services)
//    .WithApplicationSettings();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}
app.UseStaticFiles();

app.UseRouting();

//app.UseAuthorization();

//app.MapRazorPages();

//app.UseServiceMonitoringUi(app.Environment);

//app.MapServiceMonitoringUi();

app.Run();
