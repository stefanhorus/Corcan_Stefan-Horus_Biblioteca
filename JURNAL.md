# Laboratorul Î4 · Corcan Stefan-Horus · nr. matricol 121200055 · varianta A0 · B1

|Parametru|Valoarea mea|
|-|-|
|A (ultima cifră a nr. matricol: 5)|A0|
|B (penultima cifră a nr. matricol: 5)|B1|
|V1 · cărți pe pagină|4|
|V2 · sortarea implicită a catalogului|preț desc.|
|V3 · filtrul din S3|gen|
|V4 · indicatorul din S2|preț mediu|

## S1 · Comenzile

* Protecția la overposting aleasă: **TryUpdateModelAsync**
* De ce am ales-o: leagă din formular doar câmpurile enumerate explicit (`CustomerID`, `BookID`, `OrderDate`), iar la Edit comanda se citește întâi din baza de date și se modifică doar acele câmpuri, deci nu se pierd coloane care nu sunt în formular (problema pe care `[Bind]` + `_context.Update` o are, văzută la Î2). Spre deosebire de un ViewModel, nu cere o clasă nouă și o mapare pentru un formular cu doar trei câmpuri. Verificat: un POST cu `OrderID=9999` în plus a creat comanda cu ID-ul generat de baza de date, iar 9999 nu există.
* Problemă găsită: `Create` generat prin scaffolding nu salva nimic (răspuns 200, fără mesaj de eroare). `Order.Customer` și `Order.Book` sunt de tip non-nullable, deci validarea MVC le consideră obligatorii, dar formularul trimite doar ID-urile. Am adăugat `[ValidateNever]` pe cele două proprietăți de navigare.
* Comenzi adăugate din interfață: Cititor 3 → *Domain-Driven Design* (2026-11-05) și Cititor 7 → *Clean Code* (data modificată apoi din Edit în 2026-11-01).

## S2 · Pagina unui autor și sortarea după autor

* Indicatorul V4 (prețul mediu al cărților autorului) este calculat cu interogarea:

Cod: `await _context.Book.Where(b => b.AuthorID == id).AverageAsync(b => (decimal?)b.Price)`, în `AuthorsController.Details`. SQL generat (din log):

```sql
SELECT ef_avg("b"."Price")
FROM "Book" AS "b"
WHERE "b"."AuthorID" = @id
```

Proiectul folosește SQLite, unde EF Core calculează media pentru `decimal` cu funcția proprie `ef_avg` (SQLite nu are tip decimal nativ); pe SQL Server aceeași interogare devine `SELECT AVG([b].[Price]) ...`. Verificare pentru Liviu Rebreanu: pagina arată 40,50, iar `select avg(Price) from Book where AuthorID = 4` direct în baza de date dă tot 40,5. Totalul de comenzi (14) e tot o interogare SQL: `SELECT COUNT(*) FROM "Order" AS "o" INNER JOIN "Book" AS "b" ON "o"."BookID" = "b"."ID" WHERE "b"."AuthorID" = @id`.

* De ce `OrderBy(b => b.Author.FullName)` aruncă o excepție? `FullName` este o proprietate calculată în C# (`=> $"{FirstName} {LastName}"`), fără setter, deci nu e mapată la nicio coloană. EF Core trebuie să traducă tot `OrderBy` în SQL, iar pentru `FullName` nu are ce coloană să folosească. Excepția obținută: `InvalidOperationException: ... .OrderBy(b => b.Inner.FullName)' could not be translated. Additional information: Translation of member 'FullName' on entity type 'Author' failed. This commonly occurs when the specified member is unmapped.`
* Cum am rezolvat: sortez după expresia din care e calculat `FullName`, scrisă cu coloane mapate: `books.OrderBy(b => b.Author!.FirstName + " " + b.Author.LastName)`. EF o traduce în `ORDER BY COALESCE("a"."FirstName", '') || ' ' || COALESCE("a"."LastName", '')` (din log), deci sortarea rămâne în SQL. Nu am folosit `AsEnumerable()`/`ToList()` înainte de sortare, pentru că asta ar aduce toate cărțile în memorie și ar strica paginarea făcută în baza de date.

## S3 · Filtrul variantei mele (V3)

* Filtrul implementat: **[ ]**
* Interogarea, cu filtrul aplicat:

```sql
[ ]
```

## S4 · Folosirea AI

* **Instrumente folosite:** [ ]
* **La ce pași:** [ ]
* **Un exemplu de cerere trimisă:**

> [ ]

* **O situație în care am corectat sau respins un răspuns AI:**

  * Ce a propus AI-ul / care era eroarea: [ ]
  * Cum mi-am dat seama că e greșit sau ineficient: [ ]
  * Codul inițial:

```csharp
[ ]
```

* Codul final:

```csharp
[ ]
```

---

## Ce nu am reușit (opțional)

[ ]
