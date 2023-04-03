using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<CustomerRepository>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(x =>
{
    x.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the bearer scheme",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey
    });
    x.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {new OpenApiSecurityScheme{Reference = new OpenApiReference
        {
            Id = "Bearer",
            Type = ReferenceType.SecurityScheme
        }}, new List<string>() }
    });
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = "your-issuer",
            ValidAudience = "your-audience",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("aaaaaaaaaaaaaavvvvvvvvvvvvvvvvvsssssssssssssss"))
        };
    });
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
    .AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
    .RequireAuthenticatedUser()
    .Build();
});

var app = builder.Build();
app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/customers", [ProducesResponseType(200, Type = (typeof(Customer)))]

([FromServices] CustomerRepository repo) =>
{
    return repo.GetAll();

});
app.MapGet("/customers{id}", ([FromServices] CustomerRepository repo, Guid id) =>
{
    var customer = repo.GetById(id);
    return customer is not null ? Results.Ok(customer) : Results.NotFound();

}); 
app.MapPost("/customers", ([FromServices] CustomerRepository repo, Customer customer) =>
{
    repo.Create(customer);
    return Results.Created($"/customers/{customer.Id}", customer);

});
app.MapPut("/customer/{id}", ([FromServices] CustomerRepository repo,Guid id, Customer updateCustomer) =>
{
    var customer = repo.GetById(id);
    if(customer is null)
    {
        return Results.NotFound();
    }
    repo.Update(updateCustomer);
    return Results.Ok(updateCustomer);

});
app.MapDelete("/customers/{id}", ([FromServices] CustomerRepository repo, Guid id) =>
{
    repo.Delete(id);
    return Results.Ok();
});
app.Run();

record Customer(Guid Id, string Fullname);


class CustomerRepository
{
    private readonly Dictionary<Guid, Customer> _customers = new();

    public void Create(Customer customer)
    {

        if (customer is null)
        {
            return;
        }
        _customers[customer.Id] = customer;
    }

    public Customer GetById(Guid id)
    {
        return _customers[id];
    }
    public List<Customer> GetAll()
    {
        return _customers.Values.ToList();
    }
    public void Update(Customer customer)
    {
        var existingCustomer = GetById(customer.Id);
            if(existingCustomer is null)
        {
            return;
        }
        _customers[customer.Id] = customer;
    }
    public void Delete(Guid id)
    {
          _customers.Remove(id);
    }


}