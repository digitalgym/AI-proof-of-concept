

using System.Data.SqlClient;

var connectionString = "server=192.168.200.28; database=Ambrose;User ID=sa;Password=Ambr0s3@kunda;MultipleActiveResultSets=True";
using (SqlConnection connection = new SqlConnection(connectionString))
{
    connection.Open();
}

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
