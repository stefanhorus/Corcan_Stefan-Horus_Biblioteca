using Microsoft.EntityFrameworkCore;
using Intalnirea1.Models;

namespace Intalnirea1.Data;

// Date de pornire pentru Întâlnirea 2 (aceleași ID-uri 1..40 ca în book-ratings.csv).
public static class LibrarySeed
{
    public static void Seed(LibraryContext context)
    {
        if (context.Book.Any()) return;   // baza de date are deja date

        var genres = new Dictionary<string, Genre>();
        foreach (var name in new[] { "Clasic universal", "Fantasy", "Istorie și eseu", "Nuvelă", "Poezie", "Polițist", "Programare", "Roman românesc", "SF" })
            genres[name] = new Genre { Name = name };
        context.Genre.AddRange(genres.Values);

        var authors = new Dictionary<string, Author>
        {
            ["Mihail Sadoveanu"] = new Author { FirstName = "Mihail", LastName = "Sadoveanu" },
            ["George Călinescu"] = new Author { FirstName = "George", LastName = "Călinescu" },
            ["Mircea Eliade"] = new Author { FirstName = "Mircea", LastName = "Eliade" },
            ["Liviu Rebreanu"] = new Author { FirstName = "Liviu", LastName = "Rebreanu" },
            ["Marin Preda"] = new Author { FirstName = "Marin", LastName = "Preda" },
            ["Camil Petrescu"] = new Author { FirstName = "Camil", LastName = "Petrescu" },
            ["Mateiu Caragiale"] = new Author { FirstName = "Mateiu", LastName = "Caragiale" },
            ["Ioan Slavici"] = new Author { FirstName = "Ioan", LastName = "Slavici" },
            ["Ion Creangă"] = new Author { FirstName = "Ion", LastName = "Creangă" },
            ["Mihai Eminescu"] = new Author { FirstName = "Mihai", LastName = "Eminescu" },
            ["George Bacovia"] = new Author { FirstName = "George", LastName = "Bacovia" },
            ["Tudor Arghezi"] = new Author { FirstName = "Tudor", LastName = "Arghezi" },
            ["Isaac Asimov"] = new Author { FirstName = "Isaac", LastName = "Asimov" },
            ["Frank Herbert"] = new Author { FirstName = "Frank", LastName = "Herbert" },
            ["Stanisław Lem"] = new Author { FirstName = "Stanisław", LastName = "Lem" },
            ["William Gibson"] = new Author { FirstName = "William", LastName = "Gibson" },
            ["Ursula K. Le Guin"] = new Author { FirstName = "Ursula K.", LastName = "Le Guin" },
            ["J.R.R. Tolkien"] = new Author { FirstName = "J.R.R.", LastName = "Tolkien" },
            ["Patrick Rothfuss"] = new Author { FirstName = "Patrick", LastName = "Rothfuss" },
            ["Terry Pratchett"] = new Author { FirstName = "Terry", LastName = "Pratchett" },
            ["Agatha Christie"] = new Author { FirstName = "Agatha", LastName = "Christie" },
            ["Arthur Conan Doyle"] = new Author { FirstName = "Arthur Conan", LastName = "Doyle" },
            ["Umberto Eco"] = new Author { FirstName = "Umberto", LastName = "Eco" },
            ["Stieg Larsson"] = new Author { FirstName = "Stieg", LastName = "Larsson" },
            ["Robert C. Martin"] = new Author { FirstName = "Robert C.", LastName = "Martin" },
            ["David Thomas"] = new Author { FirstName = "David", LastName = "Thomas" },
            ["Martin Kleppmann"] = new Author { FirstName = "Martin", LastName = "Kleppmann" },
            ["Jon Skeet"] = new Author { FirstName = "Jon", LastName = "Skeet" },
            ["Eric Evans"] = new Author { FirstName = "Eric", LastName = "Evans" },
            ["Yuval Noah Harari"] = new Author { FirstName = "Yuval Noah", LastName = "Harari" },
            ["Jared Diamond"] = new Author { FirstName = "Jared", LastName = "Diamond" },
            ["Lucian Boia"] = new Author { FirstName = "Lucian", LastName = "Boia" },
            ["Nicolae Steinhardt"] = new Author { FirstName = "Nicolae", LastName = "Steinhardt" },
            ["Ana Exemplu"] = new Author { FirstName = "Ana", LastName = "Exemplu" },
            ["George Orwell"] = new Author { FirstName = "George", LastName = "Orwell" },
            ["Jane Austen"] = new Author { FirstName = "Jane", LastName = "Austen" },
            ["Feodor Dostoievski"] = new Author { FirstName = "Feodor", LastName = "Dostoievski" },
        };
        context.Author.AddRange(authors.Values);
        context.SaveChanges();

        // De ce SaveChanges după fiecare carte? (întrebare pentru Misiunea 1a)
        var books = new List<Book>
        {
            new() { Title = "Baltagul", Year = 1930, Price = 32m, Author = authors["Mihail Sadoveanu"], Genre = genres["Roman românesc"],
                    Description = "O femeie din munți pornește pe urmele soțului dispărut și reconstituie, cu răbdare și intuiție, drumul acestuia." }, // 1
            new() { Title = "Enigma Otiliei", Year = 1938, Price = 45m, Author = authors["George Călinescu"], Genre = genres["Roman românesc"],
                    Description = "Un tânăr orfan ajunge în casa unei familii bucureștene în care moștenirea și iubirea se amestecă." }, // 2
            new() { Title = "Maitreyi", Year = 1933, Price = 38m, Author = authors["Mircea Eliade"], Genre = genres["Roman românesc"],
                    Description = "Un inginer european în India trăiește o iubire care îi pune la încercare convingerile și cultura." }, // 3
            new() { Title = "Ion", Year = 1920, Price = 42m, Author = authors["Liviu Rebreanu"], Genre = genres["Roman românesc"],
                    Description = "Un țăran ardelean este sfâșiat între dorința de pământ și chemarea iubirii." }, // 4
            new() { Title = "Moromeții", Year = 1955, Price = 48m, Author = authors["Marin Preda"], Genre = genres["Roman românesc"],
                    Description = "Viața unei familii din Câmpia Dunării, surprinsă în anii în care lumea satului începe să se schimbe." }, // 5
            new() { Title = "Ultima noapte de dragoste, întâia noapte de război", Year = 1930, Price = 40m, Author = authors["Camil Petrescu"], Genre = genres["Roman românesc"],
                    Description = "Un ofițer analizează obsesiv gelozia care îl macină, înainte ca războiul să-i schimbe perspectiva." }, // 6
            new() { Title = "Pădurea spânzuraților", Year = 1922, Price = 39m, Author = authors["Liviu Rebreanu"], Genre = genres["Roman românesc"],
                    Description = "Un ofițer prins între datorie și conștiință în timpul Primului Război Mondial." }, // 7
            new() { Title = "Craii de Curtea-Veche", Year = 1929, Price = 36m, Author = authors["Mateiu Caragiale"], Genre = genres["Roman românesc"],
                    Description = "Trei boemi rătăcesc prin Bucureștiul decadent, într-o proză rafinată și misterioasă." }, // 8
            new() { Title = "Mara", Year = 1894, Price = 34m, Author = authors["Ioan Slavici"], Genre = genres["Roman românesc"],
                    Description = "O văduvă întreprinzătoare își crește copiii într-un oraș de graniță, cu socoteli și sacrificii." }, // 9
            new() { Title = "Amintiri din copilărie", Year = 1892, Price = 28m, Author = authors["Ion Creangă"], Genre = genres["Nuvelă"],
                    Description = "Năzbâtiile unui copil dintr-un sat moldovenesc, povestite cu umor și nostalgie." }, // 10
            new() { Title = "Moara cu noroc", Year = 1881, Price = 26m, Author = authors["Ioan Slavici"], Genre = genres["Nuvelă"],
                    Description = "Un cârciumar ambițios se lasă atras de un om periculos, iar dorința de câștig îl schimbă." }, // 11
            new() { Title = "Luceafărul și alte poezii", Year = 1883, Price = 30m, Author = authors["Mihai Eminescu"], Genre = genres["Poezie"],
                    Description = "O selecție din lirica eminesciană, de la meditație filozofică la poezia de dragoste." }, // 12
            new() { Title = "Plumb", Year = 1916, Price = 24m, Author = authors["George Bacovia"], Genre = genres["Poezie"],
                    Description = "Poeme ale cenușiului și ale ploii, despre singurătate și apăsarea orașului de provincie." }, // 13
            new() { Title = "Cuvinte potrivite", Year = 1927, Price = 27m, Author = authors["Tudor Arghezi"], Genre = genres["Poezie"],
                    Description = "Poezie care caută frumusețea în lucrurile aspre și întrebări despre credință." }, // 14
            new() { Title = "Fundația", Year = 1951, Price = 44m, Author = authors["Isaac Asimov"], Genre = genres["SF"],
                    Description = "Un matematician prevede căderea unui imperiu galactic și pune la cale un plan pe o mie de ani." }, // 15
            new() { Title = "Dune", Year = 1965, Price = 55m, Author = authors["Frank Herbert"], Genre = genres["SF"],
                    Description = "Pe o planetă deșertică, controlul unei resurse rare decide soarta unui imperiu." }, // 16
            new() { Title = "Solaris", Year = 1961, Price = 41m, Author = authors["Stanisław Lem"], Genre = genres["SF"],
                    Description = "Cercetătorii unei stații spațiale încearcă să înțeleagă un ocean care pare să gândească." }, // 17
            new() { Title = "Neuromancer", Year = 1984, Price = 43m, Author = authors["William Gibson"], Genre = genres["SF"],
                    Description = "Un hacker decăzut primește o ultimă misiune într-o lume dominată de corporații și rețele." }, // 18
            new() { Title = "Mâna stângă a întunericului", Year = 1969, Price = 42m, Author = authors["Ursula K. Le Guin"], Genre = genres["SF"],
                    Description = "Un trimis pe o planetă înghețată învață să înțeleagă o societate fără genuri fixe." }, // 19
            new() { Title = "Hobbitul", Year = 1937, Price = 39m, Author = authors["J.R.R. Tolkien"], Genre = genres["Fantasy"],
                    Description = "Un hobbit comod pleacă, fără voia lui, într-o aventură cu pitici și un dragon." }, // 20
            new() { Title = "Numele vântului", Year = 2007, Price = 52m, Author = authors["Patrick Rothfuss"], Genre = genres["Fantasy"],
                    Description = "Un erou legendar își povestește singur adevărata viață, de la copilărie la academia de magie." }, // 21
            new() { Title = "Un vrăjitor din Terramare", Year = 1968, Price = 37m, Author = authors["Ursula K. Le Guin"], Genre = genres["Fantasy"],
                    Description = "Un tânăr vrăjitor eliberează o umbră din greșeală și trebuie să o înfrunte." }, // 22
            new() { Title = "Culoarea magiei", Year = 1983, Price = 35m, Author = authors["Terry Pratchett"], Genre = genres["Fantasy"],
                    Description = "Un vrăjitor nepriceput însoțește primul turist din Lumea Disc, într-o parodie plină de umor." }, // 23
            new() { Title = "Crima din Orient Express", Year = 1934, Price = 33m, Author = authors["Agatha Christie"], Genre = genres["Polițist"],
                    Description = "Un detectiv celebru investighează o crimă comisă într-un tren blocat de zăpadă." }, // 24
            new() { Title = "Câinele din Baskerville", Year = 1902, Price = 29m, Author = authors["Arthur Conan Doyle"], Genre = genres["Polițist"],
                    Description = "O legendă de familie și un câine misterios pun la încercare logica unui detectiv." }, // 25
            new() { Title = "Numele trandafirului", Year = 1980, Price = 49m, Author = authors["Umberto Eco"], Genre = genres["Polițist"],
                    Description = "Într-o mănăstire medievală, un călugăr investighează morți legate de o bibliotecă secretă." }, // 26
            new() { Title = "Fata cu un tatuaj de dragon", Year = 2005, Price = 46m, Author = authors["Stieg Larsson"], Genre = genres["Polițist"],
                    Description = "Un jurnalist și o hackeriță cercetează dispariția unei tinere de acum patruzeci de ani." }, // 27
            new() { Title = "Clean Code", Year = 2008, Price = 189m, Author = authors["Robert C. Martin"], Genre = genres["Programare"],
                    Description = "Principii și exemple pentru cod lizibil, ușor de testat și de întreținut." }, // 28
            new() { Title = "The Pragmatic Programmer", Year = 1999, Price = 175m, Author = authors["David Thomas"], Genre = genres["Programare"],
                    Description = "Obiceiuri și tehnici practice pentru programatori care vor să-și perfecționeze meseria." }, // 29
            new() { Title = "Designing Data-Intensive Applications", Year = 2017, Price = 210m, Author = authors["Martin Kleppmann"], Genre = genres["Programare"],
                    Description = "Cum funcționează bazele de date, replicarea și sistemele distribuite din spatele aplicațiilor moderne." }, // 30
            new() { Title = "C# in Depth", Year = 2019, Price = 195m, Author = authors["Jon Skeet"], Genre = genres["Programare"],
                    Description = "O incursiune detaliată în evoluția limbajului C# și în felul în care funcționează intern." }, // 31
            new() { Title = "Domain-Driven Design", Year = 2003, Price = 220m, Author = authors["Eric Evans"], Genre = genres["Programare"],
                    Description = "Cum modelăm software-ul complex în jurul limbajului și regulilor domeniului de business." }, // 32
            new() { Title = "Sapiens", Year = 2011, Price = 58m, Author = authors["Yuval Noah Harari"], Genre = genres["Istorie și eseu"],
                    Description = "O istorie a speciei umane, de la vânătorii-culegători la revoluțiile științifice." }, // 33
            new() { Title = "Arme, germeni și oțel", Year = 1997, Price = 62m, Author = authors["Jared Diamond"], Genre = genres["Istorie și eseu"],
                    Description = "De ce unele societăți au dominat altele? Un răspuns din geografie, biologie și istorie." }, // 34
            new() { Title = "Istoria critică a românilor", Year = 1997, Price = 47m, Author = authors["Lucian Boia"], Genre = genres["Istorie și eseu"],
                    Description = "O analiză a felului în care miturile istorice au modelat conștiința națională." }, // 35
            new() { Title = "Jurnalul fericirii", Year = 1991, Price = 54m, Author = authors["Nicolae Steinhardt"], Genre = genres["Istorie și eseu"],
                    Description = "Mărturia unui intelectual despre detenția politică și despre transformarea sa interioară." }, // 36
            new() { Title = "Carte nouă, fără evaluări", Year = 2021, Price = 39m, Author = authors["Ana Exemplu"], Genre = genres["Istorie și eseu"],
                    Description = "Eseu fictiv folosit ca exemplu de carte recentă, fără evaluări suficiente (pentru discuția cold-start)." }, // 37
            new() { Title = "1984", Year = 1949, Price = 37m, Author = authors["George Orwell"], Genre = genres["Clasic universal"],
                    Description = "Într-un stat totalitar, un funcționar încearcă să-și păstreze libertatea de a gândi." }, // 38
            new() { Title = "Mândrie și prejudecată", Year = 1813, Price = 31m, Author = authors["Jane Austen"], Genre = genres["Clasic universal"],
                    Description = "O tânără inteligentă și un aristocrat orgolios își depășesc primele impresii." }, // 39
            new() { Title = "Crimă și pedeapsă", Year = 1866, Price = 44m, Author = authors["Feodor Dostoievski"], Genre = genres["Clasic universal"],
                    Description = "Un student sărac comite o crimă și se confruntă apoi cu propria conștiință." }, // 40
        };
        foreach (var book in books)
        {
            context.Book.Add(book);
            context.SaveChanges();
        }

        var publishers = new[] { new Publisher { Name = "Polirom" }, new Publisher { Name = "Humanitas" },
                                 new Publisher { Name = "Nemira" }, new Publisher { Name = "Art" } };
        var rnd = new Random(7);
        foreach (var book in books)
            book.Publishers.Add(publishers[rnd.Next(publishers.Length)]);

        var customers = Enumerable.Range(1, 20).Select(i => new Customer
        {
            Name = $"Cititor {i}",
            Adress = $"Str. Bibliotecii nr. {i}",
            BirthDate = new DateTime(1980 + i, 1 + i % 12, 1 + i % 27)
        }).ToList();
        context.Customer.AddRange(customers);

        for (int i = 0; i < 300; i++)
            context.Order.Add(new Order
            {
                Customer = customers[rnd.Next(customers.Count)],
                Book = books[rnd.Next(books.Count)],
                OrderDate = DateTime.Today.AddDays(-rnd.Next(1, 365))
            });

        context.SaveChanges();
    }
}
