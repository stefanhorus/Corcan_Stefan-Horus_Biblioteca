# Laboratorul I4 · Corcan Stefan-Horus · nr. matricol 121200055 · varianta A0 · B1

|Parametru|Valoarea mea|
|-|-|
|A (ultima cifra: 5)|A0|
|B (penultima cifra: 5)|B1|
|V1 · carti pe pagina|4|
|V2 · sortarea implicita|pret descrescator|
|V3 · filtrul din S3|gen|
|V4 · indicatorul din S2|pret mediu|

## S1 · Comenzile

* Protectia la overposting: **TryUpdateModelAsync**
* De ce: leaga doar campurile pe care le aleg eu (`CustomerID`, `BookID`, `OrderDate`). La Edit comanda e citita intai din baza de date, deci nu se pierd coloanele care nu sunt in formular. Un ViewModel ar fi fost prea mult pentru un formular cu 3 campuri. Am testat cu un `OrderID=9999` adaugat in POST si a fost ignorat.
* Problema gasita: `Create` generat prin scaffolding nu salva nimic si nu dadea nicio eroare. `Customer` si `Book` din `Order` nu sunt nullable, asa ca validarea le cerea obligatoriu, desi formularul trimite doar ID-urile. Am pus `[ValidateNever]` pe ele.
* Am adaugat din interfata 2 comenzi: Cititor 3 → Domain-Driven Design si Cititor 7 → Clean Code.

## S2 · Pagina unui autor si sortarea dupa autor

* Pretul mediu e calculat in SQL cu `AverageAsync` pe cartile autorului:

```sql
SELECT ef_avg("b"."Price")
FROM "Book" AS "b"
WHERE "b"."AuthorID" = @id
```

Folosesc SQLite, de aceea apare `ef_avg`. Pe SQL Server ar fi `AVG`. Am verificat la Rebreanu: pagina arata 40,50, la fel ca in baza de date.

* De ce `OrderBy(b => b.Author.FullName)` da exceptie: `FullName` e calculat in C# si nu exista ca o coloana in baza de date, deci EF nu are cum sa-l traduca in SQL. Eroarea: `Translation of member 'FullName' on entity type 'Author' failed`.
* Cum am rezolvat: sortez dupa `FirstName + " " + LastName`, care se traduce in SQL. Nu am pus `ToList()` inainte de sortare, pentru ca asa ar fi adus toate cartile in memorie si ar fi stricat paginarea.

## S3 · Filtrul variantei mele (V3)

* Filtrul: **gen**, cu o lista care are si optiunea „Toate”. Merge impreuna cu cautarea si sortarea, iar linkurile de sortare si butoanele de pagina pastreaza toti parametrii.
* Interogarea (cautare „a” + gen Roman romanesc + sortare dupa titlu):

```sql
SELECT COUNT(*)
FROM "Book" AS "b"
WHERE instr("b"."Title", @searchString) > 0 AND "b"."GenreID" = @genreId

SELECT "b0"."ID", "b0"."Title", CASE
    WHEN "a"."ID" IS NOT NULL THEN COALESCE("a"."FirstName", '') || ' ' || COALESCE("a"."LastName", '')
    ELSE ''
END, CASE
    WHEN "g"."ID" IS NOT NULL THEN "g"."Name"
    ELSE ''
END, "b0"."Price", (
    SELECT COUNT(*)
    FROM "Order" AS "o"
    WHERE "b0"."ID" = "o"."BookID")
FROM (
    SELECT "b"."ID", "b"."AuthorID", "b"."GenreID", "b"."Price", "b"."Title"
    FROM "Book" AS "b"
    WHERE instr("b"."Title", @searchString) > 0 AND "b"."GenreID" = @genreId
    ORDER BY "b"."Title", "b"."ID"
    LIMIT @p3 OFFSET @p
) AS "b0"
LEFT JOIN "Author" AS "a" ON "b0"."AuthorID" = "a"."ID"
LEFT JOIN "Genre" AS "g" ON "b0"."GenreID" = "g"."ID"
ORDER BY "b0"."Title", "b0"."ID"
```

* Scenariul meu: gen Roman romanesc, cautare „a”, sortare dupa titlu. Au iesit 7 carti pe 2 pagini (4 pe pagina). Am verificat si in baza de date, tot 7. Cand schimb pagina sau sortarea, filtrul si cautarea raman.
* Am adaugat `ThenBy(b => b.ID)` la sortare, pentru ca sunt carti cu acelasi pret si fara asta o carte putea sa apara pe doua pagini.

## S4 · Folosirea AI

* **Ce am folosit:** Claude (Claude Code).
* **La ce pasi:** la toti, si la jurnal. Am verificat fiecare pas in aplicatie si cu SQL inainte de commit.
* **Exemplu de cerere:**

> fa tot ce scrie acolo ca e de facut

* **O situatie in care AI-ul a gresit:**

  * Ce s-a intamplat: la I1 preturile se salvau gresit, 49.00 aparea ca 4900,00. Mac-ul meu e setat pe romana, unde virgula e separatorul zecimal, asa ca punctul era citit ca separator de mii.
  * Cum mi-am dat seama: dupa ce am adaugat carti, preturile din lista erau de 100 de ori mai mari. Nu aparea nicio eroare, se vedea doar comparand valorile.
  * Codul initial:

```csharp
app.UseHttpsRedirection();
app.UseRouting();
```

* Codul final:

```csharp
app.UseHttpsRedirection();

// Preturile folosesc punctul zecimal (45.50), indiferent de setarile regionale ale sistemului.
app.UseRequestLocalization("en-US");

app.UseRouting();
```

* **Alte corecturi:** AI-ul a creat un `.slnx` in acelasi folder cu `.csproj`, iar `dotnet build` dadea eroarea MSB1011, asa ca l-am sters. Am mai corectat `Create` de la comenzi (S1) si sortarea instabila de la paginare (S3).

---

## Ce nu am reusit (optional)

* Am folosit **SQLite** in loc de SQL Server, pentru ca SQL Server nu merge pe Mac. Codul e acelasi, difera doar SQL-ul generat.
