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

```sql
[ ]
```

* De ce `OrderBy(b => b.Author.FullName)` aruncă o excepție? [ ]
* Cum am rezolvat: [ ]

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
