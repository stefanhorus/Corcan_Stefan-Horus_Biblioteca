# Fisa 7 · Intalnirea 7 · raspunsuri

## Problema zilei

| Intrebare | Raspuns |
|-|-|
| Ce a raspuns modelul? | Nu a dat niciun titlu si niciun pret. A spus ca nu are informatii despre cartile autorului din biblioteca FSEGA si ne-a trimis sa intrebam librariile sau site-ul FSEGA. In plus, a inventat ce inseamna FSEGA. |
| Ce e corect si ce nu, fata de /Books? | Corect e ca a recunoscut ca nu poate raspunde la intrebare. Gresit e ca a dat o explicatie inventata despre FSEGA (a halucinat). Preturile nu le stie, pentru ca nu i-am dat acces la catalogul nostru de carti. |
| Ce i-ar trebui modelului ca sa raspunda corect? | Are nevoie de tool-uri pe care sa le poata apela, de exemplu un tool care cauta o carte in catalog si ii afla pretul. |

## Misiunea 1 · Biblioteca devine server MCP

* Portul https: **7245**.
* Fisierele de pe Moodle (`Mcp/LibraryTools.cs` si `Program.cs` pentru agent) nu le aveam, asa ca le-am scris dupa descrierea din fisa.

### Partea C · Ce vede clientul

| Tool | Parametri (nume : tip) | De unde vine descrierea afisata? |
|-|-|-|
| search_books | query : string | Din atributul `[Description]` de pe metoda `SearchBooks` si de pe parametrul `query`, din `LibraryTools.cs`. Numele vine din `[McpServerTool(Name = "search_books")]`. |
| get_book_details | id : integer | Tot din `[Description]` pe metoda si pe parametrul `id`. |
| recommend_books | readerId : integer, top : integer (implicit 5) | Tot din `[Description]`. `top` nu e obligatoriu, pentru ca are valoare implicita in C#. |

| Intrebare | Raspuns |
|-|-|
| `LibraryContext db` apare in schema JSON? De ce? | **Nu.** In schema apare doar `query`. `LibraryContext` e un serviciu inregistrat in aplicatie, asa ca serverul il primeste singur prin dependency injection. Clientul nu trebuie si nici nu poate sa-l trimita. |
| Ce a intors `search_books` cu „Rebreanu”? In ce format? | Doua carti: Ion (ID 4, 1920, 42 lei) si Padurea spanzuratilor (ID 7, 1922, 39 lei), ambele Roman romanesc. Rezultatul e **text**, iar textul e **JSON** (o lista de obiecte cu ID, titlu, autor, gen, an si pret). |
| Cautati `xyz`. Ce intoarce? E eroare? | Intoarce o lista goala `[]`. **Nu e eroare** (`IsError = false`): cautarea a mers, doar ca nu exista nicio carte. |
| Ce informatii despre cod are clientul? Vede LINQ-ul? | **Nu vede codul.** Vede doar numele tool-ului, descrierea si schema parametrilor. Ce se intampla pe server (interogarea LINQ, baza de date) ramane ascuns. |

## Misiunea 2 · Agentul Bibliotecii

Model: `llama3.2` (Ollama, local). Fiecare intrebare rulata o singura data.

| # | Intrebarea | Tool-uri apelate | Raspunsul e corect? |
|-|-|-|-|
| 1 | Ce carti de Rebreanu are biblioteca? | `search_books({"query":"Rebreanu"})` | **Da.** Ion (1920, 42 lei) si Padurea spanzuratilor (1922, 39 lei), exact ca in /Books. |
| 2 | Spune-mi mai multe despre romanul Ion: anul, genul si pretul. | `search_books({"query":"Ion"})` | **Partial.** Anul 1920 e corect, genul l-a zis aproximativ („romanesc”), iar pretul (42 lei) nu l-a spus, desi il primise de la tool. Nu a apelat `get_book_details`. In plus, a inventat actiunea romanului („un accident”, „cautarea cunoasterii”). |
| 3 | Ce carti ii recomanzi cititorului 5? | `recommend_books({"readerId":null,"top":5})` | **Nu.** A trimis `readerId` gol, serverul a dat eroare (`The JSON value could not be converted to System.Int32`), iar modelul a inventat carti care nu sunt in catalog (Harry Potter, Jo Nesbo etc.). |
| 4 | Ce carti ii recomanzi unui cititor nou, cu ID-ul 999? | `recommend_books({"top":"5","readerId":"999"})` | **Da, cartile sunt corecte:** Cainele din Baskerville, Domain-Driven Design, Fata cu un tatuaj de dragon, Dune, Istoria critica a romanilor (cold start). Descrierile adaugate de el sunt insa inventate (de ex. „Fata cu un tatuaj de dragon” nu e fantasy). |
| 5 | Care este cea mai scumpa carte din catalog? | `search_books({"query":"scumpa"})` | **Nu.** Nu exista un tool pentru preturi, asa ca a cautat cuvantul „scumpa”, a primit `[]` si a inventat „Cartea de valoare” de Alexandru Ioan Cuza, 5000 lei. Corect: Domain-Driven Design, 220 lei. |
| 6 | Care este capitala Frantei? | `search_books({"query":"Paris"})` | **Raspunsul (Paris) e corect,** dar apelul a fost inutil: a cautat „Paris” in catalog, a primit `[]` si a raspuns din ce stia deja. |

## Misiunea 3 · Un tool nou si descrierea lui

### Partea A · Tool-ul nou

Am activat `top_books_by_price` si am scris `books_by_genre` (cartile unui gen, cautate dupa numele genului).

| Intrebarea | Tool-uri apelate | Raspunsul e corect? |
|-|-|-|
| Care este cea mai scumpa carte din catalog? | `top_books_by_price({"top":1})` | **Da.** Domain-Driven Design, 220 lei. Inainte, fara tool-ul asta, inventase o carte de 5000 lei. |
| Ce carti SF are biblioteca? | `books_by_genre({"genre":"SF"})` | **Da.** Dune (55), Fundatia (44), Mana stanga a intunericului (42), Neuromancer (43), Solaris (41), exact ca in baza de date. |

### Partea B · Descrierea conteaza

Intrebarea 1 („Ce carti de Rebreanu are biblioteca?”), o singura rulare pentru fiecare schimbare.

| # | Schimbarea | Ce a facut agentul? |
|-|-|-|
| B1 | descrierea devine „Functia 1.” | A apelat tot `search_books("Rebreanu")` si a dat cele 2 carti corecte, dar a inventat ce e in ele („un barbat care calatoreste in Italia”) si nu a mai spus preturile. |
| B2 | fara `[Description]` pe `query` | A apelat tot corect `search_books("Rebreanu")` si a dat cele 2 carti cu anii, dar fara preturi. |
| B3 | numele devine `f1` | A apelat `f1("Rebreanu")` si a raspuns corect, cu ani si preturi. A ghicit din descriere si din parametrul `query`. |
| B4 | descrierea spune „Intoarce lista de autori.” | A apelat tot `search_books("Rebreanu")`, desi descrierea era gresita, si a dat cele 2 carti cu anii. |

Concluzia: la noi agentul a ghicit de fiecare data tool-ul bun, pentru ca are doar 5 tool-uri si celelalte nu se potrivesc deloc la intrebare. Cand dispare o informatie, se sprijina pe ce a ramas: numele `search_books` (la B1, B2, B4) sau descrierea si parametrul `query` (la B3). Dar raspunsurile au fost mai slabe: la B1 a inventat continutul, la B1, B2 si B4 a lasat deoparte preturile. Cu mai multe tool-uri asemanatoare, o descriere gresita sau goala l-ar face sa aleaga gresit. Modelul vede doar numele, descrierea si parametrii, deci de ele depinde totul.
