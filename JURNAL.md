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

## A3 · API de recomandări cu cold start

* Endpoint: `GET /api/v2/readers/{id}/recommendations?top=N` (`N` limitat între 1 și 20, implicit 5), pragul pentru cold start **5 evaluări** (A0). Cererile sunt în `Intalnirea1.http`.
* **Cititorul 5** (a evaluat 23 de cărți) primește recomandări personalizate de la model, toate cu `"fallback": false`: Designing Data-Intensive Applications (4,22), Fata cu un tatuaj de dragon (3,74), Moromeții (2,97), Numele vântului (2,79), Moara cu noroc (2,65). Cărțile deja evaluate de el (de ex. Domain-Driven Design, care era pe primul loc pe pagina /Recommendations) nu mai apar. Cu `?top=3` primește primele 3; cu `?top=100` primește 16, adică toate cărțile neevaluate pentru care modelul are un scor.
* **Cititorul 999** nu există în datele de antrenare, deci modelul întoarce `NaN` pentru toate cărțile și lista personalizată e goală. Se aplică cold start-ul: cele mai bine evaluate cărți cu cel puțin 5 evaluări, toate cu `"fallback": true`, iar `score` este media notelor, nu o predicție: Câinele din Baskerville (4,14), Domain-Driven Design (3,81), Fata cu un tatuaj de dragon (3,81), Dune (3,74), Istoria critică a românilor (3,72). Am verificat cu SQL direct (`GROUP BY BookID HAVING COUNT(*) >= 5 ORDER BY AVG(Stars) DESC`).
* Diferența: 5 primește recomandări personale (scoruri prezise, fără cărțile citite), 999 primește aceeași listă de „populare” ca orice cititor nou.
* Abatere: proiectul meu nu are grupul `/api/v2/books` și fișierul `.http` de la Î3, așa că am adăugat endpoint-ul direct în `Program.cs` și am creat `Intalnirea1.http`.

## A4 · Clasificarea genului din descriere

Proiectul consolă este în `Biblioteca.ML.GenreTraining/` (exclus din compilarea aplicației MVC din `Intalnirea1.csproj`). `book-descriptions.csv` are 1270 de descrieri în 9 genuri; cele mai puține exemple sunt la Poezie (112) și Nuvelă (110), restul au 142–152.

* **MicroAccuracy: 74,7% · MacroAccuracy: 74,5%.**
* De ce diferă: MicroAccuracy numără toate predicțiile corecte din setul de test, deci fiecare carte contează la fel și clasele cu multe exemple cântăresc mai mult. MacroAccuracy calculează acuratețea separat pentru fiecare gen și face media, deci fiecare gen contează la fel. Poezie (recall 0,64) și Nuvelă (0,63) sunt și cele mai slab recunoscute, ceea ce trage în jos media pe genuri. La mine diferența e mică (0,2 puncte procentuale), pentru că genurile sunt aproape echilibrate în date (110–152 de exemple fiecare).
* Matricea de confuzie (setul de test). Cele mai dese confuzii:
  * **Poezie ↔ Nuvelă**: 6 poezii prezise ca nuvele, 5 nuvele prezise ca poezii;
  * **Programare → SF** (6) și **SF → Programare** (4), plus **Programare → Istorie și eseu** (5);
  * **Clasic universal → Roman românesc** (6).

  Este logic: descrierile (sintetice) amestecă intenționat teme între genuri — nuvelele și poeziile folosesc același vocabular („amintirile”, „metafore”, „o întâmplare”), cărțile de programare și SF au în comun „inteligența artificială”, „tehnologia”, „stație spațială”, iar romanele clasice și cele românești au aceleași teme („moștenirea”, „clasa socială”, „vinovăția”). Fantasy și Polițist au vocabular distinctiv („dragon”, „vrăjitor”; „suspecții”, „alibiul”) și au cea mai mare precizie (0,82 și 0,92).
* **Test cu descrierea lui Dune** din seed („Pe o planetă deșertică, controlul unei resurse rare decide soarta unui imperiu.”): **SF, 79% încredere** — corect. Am testat și din browser, cu butonul „Sugerează genul” din formularul Carte nouă. Alte cărți din catalog: Câinele din Baskerville → Polițist 99,9%, Plumb → Poezie 93%, Hobbitul → Fantasy 41% (corect, dar nesigur); **Clean Code → SF 48% (greșit, ar fi Programare)**, pentru că descrierea lui scurtă nu conține cuvintele din datele de antrenare pentru Programare.
* **Descriere ambiguă** „un detectiv pe o stație spațială”: **SF, 82% încredere**. Modelul e destul de sigur pe SF deși „detectiv” trimite la Polițist: „stație spațială” apare foarte des în exemplele SF, iar „detectiv” singur apare rar. Încrederea mare nu înseamnă că predicția e corectă — e doar cea mai mare probabilitate dintre cele 9 genuri.
* Cerere cu descriere goală → răspuns 400 „Descrierea este goală.”, iar pagina afișează „Scrieți o descriere.”.

## S1 · Jurnalul recomandărilor

* Entitatea `RecommendationLog` (`ReaderId`, `CreatedAt` în UTC, `BookIds` — ID-urile cărților întoarse, în ordine, separate prin virgulă — și `Fallback`, adică dacă s-a aplicat cold start), migrarea `RecommendationLogs`. Endpoint-ul `/api/v2/readers/{id}/recommendations` salvează o înregistrare la fiecare apel, după ce calculează lista.
* Pagina **Admin/Log** (link din Administrare): tabel paginat cu `PaginatedList` din Î4 (10 pe pagină), filtru după cititor și interval de date (De la / Până la, ziua „până la” inclusă), sortare după dată (implicit cele mai noi primele; click pe „Data” inversează). Filtrarea, sortarea și paginarea sunt în SQL, iar link-urile de sortare și de paginare păstrează toți parametrii.
* Test: 18 apeluri API (cititorii 5, 7, 12, 42 și 999) → 18 înregistrări, 2 pagini; cititorul 999 apare cu cold start „da”. Pentru filtrul pe interval am adăugat temporar 3 înregistrări cu date din 1–3 noiembrie direct în baza de date: filtrul cititor 5 + 1–3 noiembrie a întors exact cele 2 înregistrări așteptate (verificat cu `SELECT COUNT(*)`), apoi le-am șters.

## S2 · Dashboard

Pagina **Admin/Dashboard** (link din Administrare), cu Chart.js 4.4.1 încărcat de pe cdnjs. Toate valorile sunt calculate în SQL (`COUNT`, `COUNT(DISTINCT)`, `AVG`, `GROUP BY`) și le-am verificat cu interogări directe în baza de date.

* Carduri: **6599** evaluări · **300** cititori distincți · media generală **3,25**.
* Distribuția notelor (grafic cu bare): 1★ 365 · 2★ 1179 · 3★ 2361 · 4★ 1854 · 5★ 840.
* Al doilea grafic, varianta **B1 — media evaluărilor pe autor** (bare orizontale, ordonate descrescător; tooltip-ul arată și numărul de evaluări): 36 de autori — Ana Exemplu nu are nicio evaluare, deci nu apare. Primii: Arthur Conan Doyle 4,14 (179 evaluări), Eric Evans 3,81, Stieg Larsson 3,81; ultimul: Robert C. Martin 2,26 (178 evaluări). Interogarea:

```sql
SELECT "a"."FirstName", "a"."LastName", AVG(CAST("r"."Stars" AS REAL)) AS "Average", COUNT(*) AS "Count"
FROM "Rating" AS "r"
INNER JOIN "Book" AS "b" ON "r"."BookID" = "b"."ID"
LEFT JOIN "Author" AS "a" ON "b"."AuthorID" = "a"."ID"
WHERE "b"."AuthorID" IS NOT NULL
GROUP BY "a"."ID", "a"."FirstName", "a"."LastName"
ORDER BY AVG(CAST("r"."Stars" AS REAL)) DESC
```

## Folosirea AI (Î5)

* **Instrument:** Claude (Claude Code, aplicația desktop Claude), la toți pașii; fiecare pas verificat în aplicație și cu SQL direct.
* **Exemplu de cerere:** „hai sa facem noua tema” (împreună cu PDF-ul laboratorului și, apoi, conținutul `book-descriptions.csv`).
* **Un răspuns AI greșit:** la S2, prima variantă a interogării pentru media pe autor construia un `record` și apoi sorta după proprietatea lui:

```csharp
var byAuthor = await db.Rating
    .Where(r => r.Book.Author != null)
    .GroupBy(r => new { r.Book.Author!.FirstName, r.Book.Author.LastName })
    .Select(g => new AuthorAverage(g.Key.FirstName + " " + g.Key.LastName,
        g.Average(r => r.Stars), g.Count()))
    .OrderByDescending(a => a.Average)
    .ToListAsync();
```

  Codul compila, dar pagina dădea eroarea 500: `InvalidOperationException: ... .OrderByDescending(e0 => e0.Average)' could not be translated`. EF nu poate traduce în SQL un `OrderBy` pe proprietatea unui obiect creat cu constructorul unui record. Varianta finală sortează pe agregat, înainte de proiecție, deci sortarea rămâne în SQL (`ORDER BY AVG(...) DESC`), iar record-ul se construiește după `ToListAsync`:

```csharp
var byAuthor = (await db.Rating
        .Where(r => r.Book.AuthorID != null)
        .GroupBy(r => new { r.Book.Author!.ID, r.Book.Author.FirstName, r.Book.Author.LastName })
        .OrderByDescending(g => g.Average(r => r.Stars))
        .Select(g => new { g.Key.FirstName, g.Key.LastName,
            Average = g.Average(r => r.Stars), Count = g.Count() })
        .ToListAsync())
    .Select(a => new AuthorAverage(a.FirstName + " " + a.LastName, a.Average, a.Count))
    .ToList();
```

  Am grupat și după `Author.ID`, ca doi autori cu același nume să nu fie amestecați.

## Abateri de la cerințe (Î5)

* SQLite în loc de SQL Server (ca la Î4); SQL-ul din jurnal este cel generat pentru SQLite.
* Nu aveam grupul `/api/v2/books` și fișierul `.http` de la Î3: endpoint-ul de recomandări e direct în `Program.cs`, iar `Intalnirea1.http` e nou.
* Proiectul consolă pentru clasificarea genului este în subfolderul `Biblioteca.ML.GenreTraining/`, exclus din compilarea aplicației MVC; nu există un fișier de soluție, pentru că un `.slnx` în același folder cu `Intalnirea1.csproj` ar face ca `dotnet build` fără argumente să eșueze (MSB1011).
