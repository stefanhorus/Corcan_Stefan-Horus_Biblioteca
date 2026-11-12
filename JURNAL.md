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

---

# Laboratorul I5 · Corcan Stefan-Horus · nr. matricol 121200055 · varianta A0 · B1

|Parametru|Valoarea mea|
|-|-|
|A3 · prag cold start (A0)|5 evaluari|
|S2 · al doilea grafic (B1)|media pe autor|

## A1 · Evaluarile in baza de date

* Am importat **6599 de evaluari** de la 300 de cititori, cat are si fisierul.
* Daca importi acelasi fisier de doua ori, evaluarile se dubleaza, pentru ca importul nu verifica duplicatele.

## A2 · Reantrenarea din aplicatie

* Am reantrenat pe 6599 de evaluari si a iesit **RMSE = 0,72**, la fel ca in aplicatia consola.
* Fara sa repornesc aplicatia, recomandarile pentru cititorul 5 s-au schimbat imediat (de exemplu, Domain-Driven Design a urcat de la 4,42 la 4,49). Merge pentru ca modelul e incarcat cu `watchForChanges: true`.

## A3 · API de recomandari cu cold start

* **Cititorul 5** primeste recomandari personale de la model (`fallback: false`), fara cartile pe care le-a evaluat deja: Designing Data-Intensive Applications, Fata cu un tatuaj de dragon, Morometii, Numele vantului, Moara cu noroc.
* **Cititorul 999** nu exista in date, deci modelul nu stie nimic despre el. Primeste cele mai bine notate carti care au minim 5 evaluari (`fallback: true`): Cainele din Baskerville, Domain-Driven Design, Fata cu un tatuaj de dragon, Dune, Istoria critica a romanilor.
* Diferenta: 5 primeste recomandari facute pentru el, iar 999 primeste aceeasi lista de carti populare ca orice cititor nou.

## A4 · Clasificarea genului din descriere

* **MicroAccuracy: 74,7% · MacroAccuracy: 74,5%.**
* De ce difera: Micro numara toate cartile la fel, iar Macro face media pe fiecare gen. Poezie si Nuvela au mai putine exemple si sunt ghicite mai prost, asa ca trag Macro in jos. La mine diferenta e mica, pentru ca genurile sunt destul de echilibrate.
* Se confunda cel mai des Poezie cu Nuvela, Programare cu SF si Clasic universal cu Roman romanesc. E logic, pentru ca descrierile lor folosesc cuvinte asemanatoare.
* **Dune:** a ghicit **SF, 79%**, corect.
* **„un detectiv pe o statie spatiala”:** **SF, 82%**. „Statie spatiala” apare foarte des la SF. Increderea mare nu inseamna neaparat ca e corect.

## S1 · Jurnalul recomandarilor

* La fiecare apel al API-ului se salveaza cititorul, data si ID-urile cartilor recomandate.
* Pagina Admin/Log are paginare (10 pe pagina), filtru dupa cititor si interval de date si sortare dupa data. Le-am testat pe toate.

## S2 · Dashboard

* Carduri: **6599** evaluari, **300** cititori, media **3,25**.
* Grafic cu distributia notelor de la 1 la 5.
* Al doilea grafic (B1) e media pe autor. Cel mai bine notat e Arthur Conan Doyle (4,14), cel mai slab Robert C. Martin (2,26).

```sql
SELECT "a"."FirstName", "a"."LastName", AVG(CAST("r"."Stars" AS REAL)) AS "Average", COUNT(*) AS "Count"
FROM "Rating" AS "r"
INNER JOIN "Book" AS "b" ON "r"."BookID" = "b"."ID"
LEFT JOIN "Author" AS "a" ON "b"."AuthorID" = "a"."ID"
WHERE "b"."AuthorID" IS NOT NULL
GROUP BY "a"."ID", "a"."FirstName", "a"."LastName"
ORDER BY AVG(CAST("r"."Stars" AS REAL)) DESC
```

## Folosirea AI (I5)

* Am folosit Claude la toti pasii si am verificat totul in aplicatie.
* **Unde a gresit:** prima varianta pentru media pe autor sorta dupa un obiect deja construit, iar EF nu putea traduce asta in SQL. Codul compila, dar pagina dadea eroarea 500.

```csharp
var byAuthor = await db.Rating
    .Where(r => r.Book.Author != null)
    .GroupBy(r => new { r.Book.Author!.FirstName, r.Book.Author.LastName })
    .Select(g => new AuthorAverage(g.Key.FirstName + " " + g.Key.LastName,
        g.Average(r => r.Stars), g.Count()))
    .OrderByDescending(a => a.Average)
    .ToListAsync();
```

* Am corectat ca sorteze inainte de a construi obiectul, asa ca sortarea ramane in SQL:

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

## Abateri de la cerinte (I5)

* SQLite in loc de SQL Server, ca la I4.
* Nu aveam API-ul de la I3, asa ca am pus endpoint-ul direct in `Program.cs` si am facut un fisier `.http` nou.
* Proiectul consola pentru clasificarea genului e in folderul `Biblioteca.ML.GenreTraining/`.

---

# Laboratorul I9 · Corcan Stefan-Horus · nr. matricol 121200055 · varianta A0 · B1

|Parametru|Valoarea mea|
|-|-|
|A0 · pragul „Foarte recomandata”|4,0|
|A0 · pragul „Recomandata”|3,0|
|B1 · tool-ul MCP din S2|books_by_year|
|Cititorul meu|1055 (1000 + 055)|

## A1 · Portalul cititorului

* Proiectul `Biblioteca.Portal` (MVC) apeleaza serviciul gRPC `Recommender` al Bibliotecii. Adresa (`https://localhost:7245`) e in `appsettings.json`, la `Biblioteca:GrpcAddress`.
* Cititorul **1055** primeste Cainele din Baskerville 4,14, Domain-Driven Design 3,81, Fata cu un tatuaj de dragon 3,81, Dune 3,74 si Istoria critica a romanilor 3,72, toate cu eticheta **„cititor nou”**.
* De ce apare eticheta: cititorul 1055 nu are nicio evaluare in baza de date si nu exista nici in datele pe care s-a antrenat modelul. Modelul nu poate prezice nimic pentru el, asa ca serviciul trece pe cold start (cele mai bine notate carti) si pune `fallback = true`. Pentru cititorul 5, care are evaluari, eticheta nu apare.

## A2 · Campul label in contract

* Am adaugat `string label = 5;` in `BookScore` (doar in `recommender.proto` din Biblioteca) si eticheta in `ToMessage`, cu pragurile A0: „Foarte recomandata” de la 4,0, „Recomandata” de la 3,0, sub asta „Neutra”, iar pentru cold start „Populara (cititor nou)”.
* **Portalul cu proto-ul vechi:** merge normal, aceleasi carti si scoruri. Nu stie de campul 5, asa ca il ignora.
* **Biblioteca.Client din I6, cu proto-ul vechi:** optiunea 1 (unary) si optiunea 2 (server streaming) merg la fel ca inainte, fara nicio eroare. Si el ignora campul nou.
* **Portalul cu proto-ul nou:** apare coloana Eticheta. Cititorul 5: Designing Data-Intensive Applications 4,22 Foarte recomandata · Fata cu un tatuaj de dragon 3,74 Recomandata · Morometii, Numele vantului, Plumb Neutra. Cititorul 1055: toate „Populara (cititor nou)”.
* **Experiment `title = 6` doar in Biblioteca:** in portal, coloana **Carte a ramas goala**, iar scorul si eticheta au aparut normal. Portalul cauta titlul la campul 2, serverul il trimite la campul 6, deci portalul nu-l gaseste, pune string gol si ignora campul 6. Nu apare nicio eroare, doar date lipsa. Am revenit la `title = 2` si titlurile au aparut din nou.
