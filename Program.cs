using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.IO;
using System.Collections.Generic;
using System.Xml;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddDbContext<CurConvDB>(opt => opt.UseInMemoryDatabase("CurrencyList"));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

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

app.MapGet("/currency/all", async (CurConvDB db) => await db.CurrencyConverts.ToListAsync());

app.MapPost("/currency/add",async(CurrencyConvert curConv, CurConvDB db) =>
{
    db.CurrencyConverts.Add(curConv);
    await db.SaveChangesAsync();
});

app.MapPut("/currency/edit/{id}", async (int id, CurrencyConvert curObj, CurConvDB db) =>
{
    var getCur = await db.CurrencyConverts.FindAsync(id);
    if (getCur is null) return Results.NotFound();
    getCur.currencyName = curObj.currencyName;
    getCur.currencyCode = curObj.currencyCode;
    getCur.excRate = curObj.excRate;

    await db.SaveChangesAsync();
    return Results.NoContent();
});

app.MapDelete("/currency/delete/{id}", async (int id, CurConvDB db) =>
{
    if (await db.CurrencyConverts.FindAsync(id) is CurrencyConvert curObj)
    {
        db.Remove(curObj);
        await db.SaveChangesAsync();
        return Results.Ok(curObj);
    }
    return Results.NotFound();
});

app.MapGet("/currency/convert", (string sourceCur,string targetCur, decimal amount) =>
{
    if (sourceCur != null && targetCur != null && amount >= 0)
    {
        decimal resAmt = 0;
        string curTy = ""; string curVal = "";
        using (StreamReader r = new StreamReader(Environment.CurrentDirectory + "\\" + "ExchangeRate.json"))
        {
            string jsonData = r.ReadToEnd();
            var excRateData = JsonSerializer.Deserialize<Dictionary<string,string>>(jsonData);

            foreach (var c in excRateData)
            {
               if (c.Key.Contains(sourceCur.ToUpper()) && c.Key.Contains(targetCur.ToUpper()))
               {
                    curTy = c.Key;
                    curVal = c.Value;
                    resAmt = Convert.ToDecimal(c.Value) * amount;
                    break;
               }
            }
        }
        return Results.Ok($"The conversion from {curTy} at exchange rate of {curVal} is: {resAmt}");
    }
    else { return Results.BadRequest(); }
});

app.MapPatch("/currency/convert/edit/", async (string sourceCur, string targetCur, decimal updateRate) =>
{
    string writeJson = "";
    string filePath = Environment.CurrentDirectory + "\\" + "ExchangeRate.json";
    if (sourceCur != null && targetCur != null && updateRate >= 0)
    {
        string curTy = ""; string curVal = "";
        //Reading to validate
        using (StreamReader r = new StreamReader(filePath))
        {
            string jsonData = r.ReadToEnd();
            var excRateData = JsonSerializer.Deserialize<Dictionary<string, string>>(jsonData);

            foreach (var c in excRateData)
            {
                if (c.Key.Contains(sourceCur.ToUpper()) && c.Key.Contains(targetCur.ToUpper()))
                {
                    curTy = c.Key;
                    curVal = c.Value;
                    excRateData[c.Key] = Convert.ToString(updateRate);
                    break;
                }
            }
            writeJson = JsonSerializer.Serialize(excRateData);
        }
        //Writing back to Json File
        File.WriteAllText(filePath, writeJson);

        return Results.Ok($"The conversion rate for {curTy} is updated from {curVal} to {updateRate}");
    }
    else { return Results.BadRequest(); }
});

app.Run();

public class CurrencyConvert
{
   [Key]
   public int curID { get; set; }
   public string currencyName {get; set;}
   public string currencyCode {get; set;}
   public decimal excRate {get; set;}
}

public class excRate
{
    public string USD_TO_INR { get; set; }
    public string INR_TO_USD { get; set; }
    public string USD_TO_EUR { get; set; }
    public string EUR_TO_USD { get; set; }
    public string INR_TO_EUR { get; set; }
    public string EUR_TO_INR { get; set; }
}

class CurConvDB: DbContext
{
    public CurConvDB(DbContextOptions options) : base(options) { }
    public DbSet<CurrencyConvert> CurrencyConverts => Set<CurrencyConvert>();
}