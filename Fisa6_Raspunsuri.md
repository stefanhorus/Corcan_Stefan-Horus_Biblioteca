# Fisa 6 · Intalnirea 6 · raspunsuri

## Problema zilei

Niciuna, pe deplin.
1. Mesaje mici: nu. JSON e text, cu numele campurilor repetate in fiecare obiect.
2. Recomandarile pe rand: nu. `GET /api/v2/readers/{id}/recommendations` intoarce toata lista o singura data, la final.
3. Contract strict: nu. API-ul nu are o descriere formala (OpenAPI), deci clientul trebuie scris de mana, dupa exemple.

## Misiunea 1 · Contractul gRPC

### Partea B

| # | Intrebare | Raspuns |
|-|-|-|
| 1 | Tipul fiecarei metode | `Recommend` e **unary** (un mesaj trimis, un mesaj primit). `StreamRecommendations` e **server streaming** (`returns (stream BookScore)`). `RateBooks` e **client streaming** (`(stream RatingInput)`). Nu avem niciuna bidirectionala (ar avea `stream` pe ambele parti). In `RecommenderGrpc.cs` apar ca `MethodType.Unary`, `MethodType.ServerStreaming`, `MethodType.ClientStreaming`. |
| 2 | Ce clase | Serverul mosteneste `Recommender.RecommenderBase`. Clientul foloseste `Recommender.RecommenderClient`. Proiectul server genereaza doar `RecommenderBase` (`GrpcServices="Server"`), iar clientul doar `RecommenderClient` (`GrpcServices="Client"`). |
| 3 | La ce folosesc `= 1`, `= 2` | Sunt numerele campurilor. In mesajul binar nu se trimit numele campurilor, ci doar numarul si valoarea (de ex. `ReaderIdFieldNumber = 1`, `WriteRawTag(8)` in `Recommender.cs`). De aceea mesajele sunt mici, iar un numar nu trebuie schimbat dupa ce aplicatia e publicata. |

### Partea C · Contractul se schimba

| Schimbarea pe server | Aplicatia veche mai functioneaza? De ce? |
|-|-|
| adaugam `string author = 5;` | **Da.** Aplicatia veche nu stie de campul 5, il ignora si citeste restul normal. |
| redenumim `title` in `book_title` (tot 2) | **Da.** Pe fir circula doar numarul 2, nu numele, deci aplicatia veche primeste titlul ca inainte. Se schimba doar numele in codul generat pentru clientii noi. |
| `float score = 3;` devine `float score = 5;` | **Nu corect.** Aplicatia veche cauta campul 3, nu il mai gaseste si foloseste valoarea implicita 0, iar campul 5 il ignora. Nu crapa, dar toate scorurile apar 0. |
| stergem campul `fallback` | **Merge, dar gresit.** Aplicatia veche primeste mereu valoarea implicita `false`, deci nu mai stie cand e o recomandare de cold start. Corect ar fi sa marcam `reserved 4;`, ca numarul sa nu fie refolosit pentru altceva. |

## Misiunea 2 · Recomandatorul servit prin gRPC

* Serviciile `RecommendationService` si `RecommenderGrpcService` sunt in `Services/`, cu namespace-ul `Intalnirea1.Services`. `MinRatings = 8`.
* Endpoint-ul REST `/api/v2/readers/{id}/recommendations` foloseste acum tot `RecommendationService`, deci REST si gRPC au aceeasi logica si acelasi prag.
* Portul https: **7245** (din `Properties/launchSettings.json`, profilul `https`).
* Clientul e proiectul `Biblioteca.Client/` (`GrpcServices="Client"`), cu meniu pentru cele 3 tipuri de apel. Arhiva cu clientul nu era in folderul primit, asa ca l-am scris dupa descrierea din fisa.

| Optiunea | Ce observam | Raspuns |
|-|-|-|
| 1 · Unary (5) | Designing Data-Intensive Applications 4,22 · Fata cu un tatuaj de dragon 3,74 · Morometii 2,97 · Numele vantului 2,79 · Moara cu noroc 2,65, toate cu `fallback=False`. | **Da**, sunt exact aceleasi carti, cu aceleasi scoruri, ca la `GET /api/v2/readers/5/recommendations`. Normal, pentru ca REST si gRPC folosesc acum acelasi `RecommendationService`. |
| 2 · Server streaming (5) | Aceleasi 5 carti, dar vin pe rand: la 150, 644, 1145, 1645 si 2146 ms. | Intre doua mesaje trec **~500 ms** (`Task.Delay(500)` din serviciu). Utilizatorul ar vedea prima carte aproape imediat si apoi lista crescand, in loc sa astepte ~2 secunde un ecran gol. |
| 3 · Client streaming (5) | Clientul trimite 3 evaluari: cartea 10 cu 5 stele, 11 cu 4, 12 cu 9. Raspuns: salvate 2, respinse 1, media 4,50. | S-au salvat **2** evaluari (in baza de date au aparut 5/10/5 si 5/11/4). Cea de **9 stele** a fost respinsa de server (`Stars is < 1 or > 5`), nu s-a salvat si nu intra in medie. |
| 1 · Unary (999) | Cainele din Baskerville 4,14 · Domain-Driven Design 3,81 · Fata cu un tatuaj de dragon 3,81 · Dune 3,74 · Istoria critica a romanilor 3,72. | `fallback=True` la toate. Cititorul 999 nu apare in datele de antrenare, deci modelul nu da niciun scor si se aplica cold start-ul: cele mai bine notate carti cu minim 8 evaluari, iar scorul e media notelor. |
| 3 · Client streaming, BookId = 999 | Prima evaluare e pentru cartea 999, care nu exista. | Cod de stare **`Unknown`**, mesaj **„Exception was thrown by handler.”**. In log-ul serverului: `SQLite Error 19: 'FOREIGN KEY constraint failed'`. Toate evaluarile se salveaza la final cu un singur `SaveChangesAsync`, deci nu s-a salvat niciuna, nici cele valide. Clientul nu afla motivul real; ar fi mai corect ca serverul sa verifice cartea si sa arunce `RpcException` cu `StatusCode.NotFound` sau `InvalidArgument`. |

## Misiunea 3 · Pipeline-ul ML scris in cod

| # | Experiment | Ce se intampla |
|-|-|-|
| E1 | referinta | MicroAccuracy **74,7%**, MacroAccuracy **74,6%**. |
| E2 | `LbfgsMaximumEntropy` | Scade la **65,3%** / **64,4%**. Alt algoritm, cu aceleasi date si setari implicite, invata mai prost aici. |
| E3 | fara `FeaturizeText` | Nu porneste: `Could not find feature column 'Features'`. Algoritmul are nevoie de un vector de numere, iar fara pasul asta textul descrierii nu devine niciodata coloana `Features`. |
| E4 | `testFraction: 0.9` (10% pentru antrenare) | Scade la **56,8%** / **56,7%**. Cu doar ~127 de exemple, modelul vede prea putine cuvinte pentru fiecare gen. |
| E5 | evaluare pe `TrainSet` | Urca la **99,7%**. Rezultatul e fals de bun: modelul e testat pe exact datele pe care le-a invatat. Pe date noi ramane la ~75%. |
