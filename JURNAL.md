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

## A3 · Validare si coduri de stare gRPC

La inceputul lui `Recommend` si `StreamRecommendations` verific cererea (in metoda `ValidateRequest`). Teste din portal:

| Test | StatusCode | Mesaj (detalii) |
|-|-|-|
| cititorul -3 | **InvalidArgument** | reader_id trebuie sa fie un numar pozitiv. |
| top = 50 | **InvalidArgument** | top poate fi cel mult 20. |
| aplicatia Biblioteca oprita | **Unavailable** | Error connecting to subchannel. |

Portalul afiseaza toate erorile in pagina, nu ca pagina de eroare. In al treilea caz, eroarea nu vine de la serviciu, ci de la client, pentru ca nu are la cine sa se conecteze. Dupa repornirea Bibliotecii, recomandarile merg din nou fara sa repornesc portalul.

## S1 · Evaluarile mele prin gRPC

* **Serverul:** `ReaderRatingsGrpcService : ReaderRatings.ReaderRatingsBase`, cu toate cele 5 metode, asincron prin `LibraryContext` si cu `context.CancellationToken`, inregistrat cu `app.MapGrpcService<ReaderRatingsGrpcService>()`. Pentru `created_at` folosesc `Timestamp.FromDateTime(DateTime.SpecifyKind(..., DateTimeKind.Utc))`, pentru ca datele din SQLite vin fara `Kind`.
* **Coduri:** `NotFound` pentru o evaluare sau o carte inexistenta, `InvalidArgument` pentru o nota in afara intervalului 1–5 sau pentru `reader_id <= 0`. Mesajele sunt in romana si spun ce e gresit.
* **Portalul:** `RatingsController` cu `ReaderRatings.ReaderRatingsClient` (prin `AddGrpcClient`) si paginile Index (citita din fluxul `ListRatings`), Create, Edit (doar nota) si Delete (cu confirmare). Erorile apar in pagina.

Teste cu cititorul meu, **1055**:

| Pas | Rezultat |
|-|-|
| adaug Baltagul (ID 1), 5 stele | salvata (evaluarea 6602) |
| adaug Ion (ID 4), 3 stele | salvata (evaluarea 6603) |
| modific Ion la 4 stele | lista arata Ion · 4 |
| sterg Baltagul (cu pagina de confirmare) | a disparut din lista; in baza de date a ramas doar Ion · 4 |
| carte inexistenta, ID 999 | **NotFound**: Cartea cu ID-ul 999 nu exista in catalog. |
| nota 7 | **InvalidArgument**: Nota trebuie sa fie intre 1 si 5 stele (ai trimis 7). |
| evaluare inexistenta (99999) | **NotFound**: Evaluarea cu ID-ul 99999 nu exista. |
| cititorul -1 | **InvalidArgument**: reader_id trebuie sa fie un numar pozitiv. |

**Intrebarea: oricine poate scrie orice ID de cititor. Ce ar trebui schimbat?** Portalul ar trebui sa aiba autentificare: cititorul se logheaza, iar ID-ul lui vine din contul logat, nu dintr-un camp din formular. Portalul trimite apoi la gRPC un token (de exemplu JWT, in header-ul `Authorization`), iar serverul gRPC verifica la fiecare apel ca `reader_id` din cerere e acelasi cu cel din token si ca evaluarea modificata sau stearsa ii apartine. Altfel raspunde cu `PermissionDenied`. Verificarea trebuie facuta pe server, nu doar in portal, pentru ca serviciul gRPC poate fi apelat si direct.

## S2 · Tool-ul MCP books_by_year (varianta B1)

* Metoda `BooksByYear` din `Mcp/LibraryTools.cs`, cu `[McpServerTool(Name = "books_by_year", ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false)]`, `[Description]` pe metoda si pe fiecare parametru, interogare asincrona cu `AsNoTracking`. Intoarce o lista de record-uri `BookByYear(Id, Title, Author, Year, Price)`, cel mult 10.
* **Erori actionabile** (`McpException`): interval inversat („Apeleaza din nou cu fromYear = 1920 si toYear = 1939”), `top` in afara 1–10 si interval fara carti (spune intervalul de ani pe care il are catalogul).

**Schema JSON (inspector, optiunea 1):**

```json
{
  "type": "object",
  "properties": {
    "fromYear": { "description": "Primul an din interval, de exemplu 1920.", "type": "integer" },
    "toYear":   { "description": "Ultimul an din interval, de exemplu 1939. Trebuie să fie mai mare sau egal cu fromYear.", "type": "integer" },
    "top":      { "description": "Câte cărți să întoarcă, între 1 și 10 (implicit 10).", "type": "integer", "default": 10 }
  },
  "required": [ "fromYear", "toYear" ]
}
```

**Rezultatul unui apel**, `books_by_year({"fromYear":1920,"toYear":1939})`, `IsError` gol (fara eroare), 10 carti: Ion 1920 · Padurea spanzuratilor 1922 · Cuvinte potrivite 1927 · Craii de Curtea-Veche 1929 · Baltagul 1930 · Ultima noapte de dragoste, intaia noapte de razboi 1930 · Maitreyi 1933 · Crima din Orient Express 1934 · Hobbitul 1937 · Enigma Otiliei 1938. Primul element arata asa: `{"id":4,"title":"Ion","author":"Liviu Rebreanu","year":1920,"price":42.0}`.

Apeluri gresite, toate cu `IsError: True`:
* `{"fromYear":1939,"toYear":1920}` → Intervalul e inversat: fromYear (1939) e mai mare decat toYear (1920). Apeleaza din nou cu fromYear = 1920 si toYear = 1939.
* `{"fromYear":1700,"toYear":1750}` → Nu exista carti aparute intre 1700 si 1750. Catalogul are carti aparute intre 1813 si 2021; alege un interval din acest domeniu.
* `{"fromYear":1900,"toYear":2000,"top":50}` → top trebuie sa fie intre 1 si 10 (ai trimis 50).

**Agentul (optiunea 2, llama3.2):**

| Intrebarea | Linia [tool] | Raspunsul | Corect? |
|-|-|-|-|
| Ce carti din catalog au aparut intre 1920 si 1939? | `books_by_year(fromYear=1920, toYear=1939, top=10)` | cele 10 carti de mai sus, cu anii | **Da** (doar cateva greseli de scriere in titluri, de ex. „Padurea spanzuratorilor”) |
| Ce carti au aparut dupa anul 2000? | `books_by_year(fromYear=2000, toYear=1939, top=10)` | „Nu am putut gasi nicio carte ... dupa anul 2000.” | **Nu.** A luat 1939 din intrebarea de dinainte, a primit eroarea de interval inversat si nu a mai incercat din nou, desi mesajul ii spunea ce sa schimbe. In catalog sunt 8 carti dupa 2000 (Domain-Driven Design 2003 ... Carte noua 2021). |
| Ce carti de Rebreanu are biblioteca? (nu ar trebui sa foloseasca tool-ul meu) | `search_books(query=Rebreanu)` | Ion (1920), Padurea spanzuratilor (1922) | **Da**, si corect nu a folosit `books_by_year`. |

## Folosirea AI (I9)

* **Instrument:** Claude (Claude Code), ca asistent la scrierea codului si la testare, la pasii A1–S2. Rezultatele din jurnal le-am obtinut rulând aplicatiile si le-am verificat in baza de date.
* **Exemplu de cerere:** „fa ce trebuie la laboratorul 9” (impreuna cu PDF-ul laboratorului).
* **O situatie in care raspunsul a fost gresit:** la S2, codul propus pentru inspector (apelul unui tool cu argumente JSON) avea un string rupt pe doua randuri, pentru ca `\n` a fost transformat intr-o linie noua reala. Build-ul a dat eroarea `CS1010: Newline in constant`. Am corectat inapoi la `Console.Write("\nAlt tool de apelat (Enter = iesire): ");`.

## Abateri de la cerinte (I9)

* Baza de date e SQLite, ca la laboratoarele anterioare.
* Portul https al Bibliotecii este **7245**, nu 7123, deci in `appsettings.json` din portal am pus `https://localhost:7245`.
* Inspectorul de pe Moodle apela doar `search_books`. Am adaugat in optiunea 1 un pas care apeleaza orice tool cu argumente JSON, ca sa pot arata rezultatul lui `books_by_year`.
* Tool-ul `books_by_genre` exista deja din Fisa 7 (Misiunea 3). Tool-ul variantei mele pentru acest laborator este `books_by_year`.
* `Biblioteca.Client` (I6) a ramas intentionat cu `recommender.proto` vechi, pentru testul de compatibilitate de la A2.

---

# Laboratorul I10 · Corcan Stefan-Horus · nr. matricol 121200055 · varianta A0 · B1

|Parametru|Valoarea mea|
|-|-|
|A0 · documentul din S2|`Docs/cont-online.md` (intrebari frecvente despre contul online)|
|B1 · experimentul din S1|modelul de embeddings: embeddinggemma vs nomic-embed-text (3 intrebari in engleza)|

## A1 · Indexul urmeaza catalogul

* In `DocumentIndex` am adaugat `IndexBookAsync` si `RemoveBookAsync`, iar `BooksController` le apeleaza dupa `SaveChangesAsync` la Create, Edit si Delete.
* Test (Agent, optiunea 3, sursa carte), cu cautarea „un fiu de crai trece prin incercari grele, pacalit de un span”:
  * inainte: Moara cu noroc 0,461 · Un vrajitor din Terramare 0,451 · Crima si pedeapsa 0,439;
  * dupa ce am adaugat **Povestea lui Harap-Alb** (Ion Creanga, 1877, cu o descriere de 2 propozitii, ID 41): pe primul loc, **0,585**;
  * dupa Edit (titlul a devenit „Povestea lui Harap-Alb (editie ilustrata)”): apare o singura data, cu titlul nou, **0,588**;
  * o carte temporara (ID 42) a aparut in cautare dupa Create si a disparut dupa Delete.

**(1) De ce cheia trebuie sa fie aceeasi (`carte-{ID}`)?** `UpsertAsync` cauta fragmentul dupa cheie. Daca cheia e aceeasi, fragmentul vechi e inlocuit. Daca ar fi diferita, la fiecare Edit s-ar adauga un fragment nou si cartea ar aparea de mai multe ori, si cu textul vechi. `RemoveBookAsync` sterge tot dupa aceasta cheie, deci altfel n-ar sterge fragmentul creat la pornire. La test, dupa Edit a ramas un singur rezultat pentru Harap-Alb.

**(2) Baza a salvat cartea, dar Ollama e oprit.** Am testat: am oprit Ollama si am adaugat o carte. Pagina a dat **eroarea 500** (`HttpRequestException: Connection refused (localhost:11434)`), dar cartea **s-a salvat** in baza (ID 43). In index **nu a intrat**: nici dupa ce am pornit Ollama din nou, cautarea nu o gasea. Baza si indexul au ramas nesincronizate pana la repornirea aplicatiei (cand `BuildAsync` reface tot indexul) sau pana la urmatorul Edit al cartii. Utilizatorul vede o eroare, desi cartea a fost adaugata. Am sters apoi cartea de test.
