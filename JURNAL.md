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

* Filtrul implementat: **gen (listă cu opțiunea „Toate”)**, parametrul `genreId` în `BooksController.Index`. Se combină cu căutarea după titlu și cu sortarea; link-urile de sortare (Titlu, Autor, Preț) și butoanele Înapoi/Înainte trimit toate `currentFilter`, `genreId` și `sortOrder`, iar formularul păstrează sortarea curentă printr-un câmp ascuns.
* Interogarea, cu filtrul aplicat (din log, pentru căutare „a” + gen „Roman românesc” + sortare după titlu, pagina 1):

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

Filtrul (`"b"."GenreID" = @genreId`), căutarea, sortarea și paginarea (`LIMIT`/`OFFSET`) sunt toate în SQL; în memorie ajung doar cele 4 rânduri ale paginii.

* Scenariul meu de test (cel puțin 2 pagini): gen **Roman românesc**, căutare **„a”**, sortare **Titlu A → Z**. Rezultă **7 cărți pe 2 pagini** (4 pe pagină, varianta A0): pagina 1 — Baltagul, Craii de Curtea-Veche, Enigma Otiliei, Maitreyi; pagina 2 — Mara, Pădurea spânzuraților, Ultima noapte de dragoste, întâia noapte de război. Am verificat direct în baza de date: `select count(*) from Book where GenreID = 8 and instr(Title, 'a') > 0` = 7. Pe pagina 2, butonul Înapoi duce la `?sortOrder=title&pageNumber=1&currentFilter=a&genreId=8`, iar un click pe „Titlu” trece la Z → A păstrând filtrul și căutarea (pagina 1 începe cu „Ultima noapte…”). Fără căutare, genul Roman românesc are 9 cărți, pe 3 pagini.
* Am adăugat `ThenBy(b => b.ID)` după orice sortare: sortarea implicită este după preț și mai multe cărți au același preț (de ex. 39,00). La valori egale, ordinea din SQL nu e garantată, deci aceeași carte putea apărea pe două pagini, iar alta pe niciuna.

## S4 · Folosirea AI

* **Instrumente folosite:** Claude (Claude Code, în aplicația desktop Claude), care a rulat comenzile `dotnet`, a modificat fișierele și a testat aplicația pornind-o local și trimițând cereri HTTP.
* **La ce pași:** la toți pașii (A1–A5, S1–S3) și la completarea jurnalului. Fiecare pas a fost verificat în aplicație (pagini, formulare) și comparat cu interogări SQL directe în baza de date înainte de commit.
* **Un exemplu de cerere trimisă:**

> fa tot ce scrie acolo ca e de facut — 121200055, Corcan Stefan-Horus (împreună cu PDF-ul laboratorului și șablonul JURNAL.md)

* **O situație în care am corectat sau respins un răspuns AI:**

  * Ce a propus AI-ul / care era eroarea: la Î1, catalogul generat prin scaffolding salva prețurile greșit: cărțile trimise cu prețurile `49.00` și `52.00` apăreau în listă ca `4900,00` și `5200,00`. Codul generat nu seta nicio cultură, iar Mac-ul are setările regionale în română, unde virgula e separatorul zecimal și punctul e separator de mii, așa că model binding-ul citea „49.00” ca 4900.
  * Cum mi-am dat seama că e greșit sau ineficient: testul de adăugare/editare (3 cărți create, una editată) a afișat în Index prețuri de ~100 de ori mai mari decât cele introduse. Codul compila și pagina se încărca fără erori, deci problema se vedea doar comparând valorile salvate cu cele trimise.
  * Codul inițial:

```csharp
app.UseHttpsRedirection();
app.UseRouting();
```

* Codul final:

```csharp
app.UseHttpsRedirection();

// Prețurile folosesc punctul zecimal (45.50), indiferent de setările regionale ale sistemului.
app.UseRequestLocalization("en-US");

app.UseRouting();
```

După fix, aceleași cereri au salvat `49.00` și `52.00`. Am refăcut baza de date ca să nu rămână prețuri greșite.

* **Alte corecturi făcute în timpul laboratorului:**
  * La A1, AI-ul a creat un fișier `Biblioteca.slnx` în același folder cu `Intalnirea1.csproj`. Cu două fișiere de proiect/soluție în același folder, `dotnet build` fără argumente dă eroarea `MSB1011: Specify which project or solution file to use`. Am renunțat la `.slnx`; jurnalul stă în rădăcina repository-ului, lângă `.csproj`.
  * La S1, `Create` din controller-ul generat pentru `Order` nu salva nimic (răspuns 200, fără mesaj de eroare), din cauza validării implicite pe proprietățile de navigare non-nullable `Customer` și `Book`. Rezolvat cu `[ValidateNever]` (detalii la S1).
  * La S3, sortarea dată în ghid (doar după preț, la varianta mea) nu era stabilă pentru cărțile cu același preț, ceea ce putea muta cărți între pagini. Am adăugat `ThenBy(b => b.ID)`.

---

## Ce nu am reușit (opțional)

* Proiectul folosește **SQLite** în loc de SQL Server, pentru că SQL Server nu rulează nativ pe macOS. Codul EF Core e același; diferă doar SQL-ul generat (de ex. `ef_avg` în loc de `AVG`, `instr` în loc de `LIKE`/`CHARINDEX`, `LIMIT/OFFSET` în loc de `OFFSET … FETCH`).

---

# Laboratorul Î5 · Corcan Stefan-Horus · nr. matricol 121200055 · varianta A0 · B1

|Parametru|Valoarea mea|
|-|-|
|A3 · pragul pentru cold start (A0)|5 evaluări|
|S2 · al doilea grafic (B1)|media evaluărilor pe autor|

## A1 · Evaluările în baza de date

* Am importat `book-ratings.csv` din pagina Administrare: **6599 de evaluări** importate (300 de cititori distincți, media 3,25). Numărul se potrivește cu rândurile de date din fișier (fără antet).
* Observație: importul nu verifică duplicatele, deci o a doua încărcare a aceluiași fișier ar dubla evaluările.

## A2 · Reantrenarea din aplicație

* Butonul „Reantrenează modelul de recomandare” a antrenat pe **6599 de evaluări, RMSE = 0,72** (aceeași valoare ca aplicația consolă din Î2).
* Fără repornirea aplicației, `/Recommendations?userId=5` s-a schimbat imediat: înainte — Domain-Driven Design 4,42 · The Pragmatic Programmer 4,09 · Designing Data-Intensive Applications 4,08 · C# in Depth 3,89 · Câinele din Baskerville 3,64; după — Domain-Driven Design 4,49 · Designing Data-Intensive Applications 4,22 · The Pragmatic Programmer 4,08 · C# in Depth 3,98 · Câinele din Baskerville 3,82. `PredictionEnginePool` a fost înregistrat cu `watchForChanges: true`, deci a reîncărcat `MLModels/BookRecommender.zip` când fișierul s-a schimbat. Scorurile diferă puțin pentru că datele vin acum din baza de date (altă ordine, deci altă împărțire train/test) și antrenarea Matrix Factorization folosește mai multe fire de execuție.
