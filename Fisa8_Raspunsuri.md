# Fisa 8 · Intalnirea 8 · raspunsuri

Echipa: Corcan Stefan-Horus

## Problema zilei (agentul din I7, fara regulament)

| # | Tool-uri apelate | Ce a raspuns? Se sprijina pe date? |
|-|-|-|
| 1 | `top_books_by_price(top=5)` | A zis ca de obicei se pot imprumuta 1–2 carti pentru 2–3 luni. **Nu**, a inventat: tool-ul apelat nu avea legatura cu intrebarea. |
| 2 | `search_books(query="intarziere")` | Nu a dat o suma, a zis sa intreb biblioteca si a inventat reguli generale. **Nu**, `search_books` a intors o lista goala. |
| 3 | `search_books(query="femeie care isi cauta sotul disparut")` | A zis ca nu are cartea si a recomandat carti inventate (de ex. „Femeia pierduta” de Tess Gerritsen). **Nu**, desi avem Baltagul. |

| Intrebare | Raspuns |
|-|-|
| De unde ar putea afla raspunsurile la 1 si 2? | Din regulamentul bibliotecii. Agentul nu il are, deci ar trebui un tool care sa caute in regulament. |
| La 3, ce cuvant a cautat? De ce nu a gasit cartea? | A cautat toata fraza „femeie care isi cauta sotul disparut”. `search_books` cauta doar in titlu si autor, cu potrivire exacta de text. Fraza nu apare in titlul Baltagul, iar descrierea cartii nu e cautata deloc. |

## Misiunea 1 · Embeddings (model `embeddinggemma`)

### Partea A

| Text | Dimensiunea | Primele 3 valori |
|-|-|-|
| Care este durata unui imprumut? | 768 | -0,0587 · 0,0197 · 0,0268 |
| (a doua oara) | 768 | -0,0587 · 0,0197 · 0,0268 (identic) |
| Care este durata unui imprumut ? (cu spatiu) | 768 | -0,0674 · 0,0241 · 0,0447 (putin diferit) |

### Partea B

| Perechea | Scorul |
|-|-|
| A–B (doua saptamani / durata unui imprumut) | 0,584 |
| C–D (cat costa intarzierea / penalitate) | 0,531 |
| B–C (durata / cat costa intarzierea) | 0,422 |
| A–E (imprumut / reteta de sarmale) | 0,232 |
| A–F (romana / engleza) | 0,595 |

| Intrebare | Raspuns |
|-|-|
| Ce pereche are scorul cel mai mare? Cuvinte comune? | **A–F, 0,595.** Nu au niciun cuvant comun, sunt in limbi diferite, dar inseamna acelasi lucru. Urmeaza A–B (0,584), si ele aproape fara cuvinte comune. |
| Ce scor are o propozitie cu ea insasi? | **1,000**, pe toata diagonala. |
| Ce observati la A–F? | Modelul compara sensul, nu cuvintele. Intrebarea in engleza e mai aproape de A decat orice alta propozitie in romana. |

### Partea C

| Cautarea | Primele 3 carti |
|-|-|
| o femeie care isi cauta sotul disparut | Baltagul 0,518 · Fata cu un tatuaj de dragon 0,439 · Un vrajitor din Terramare 0,399 |
| o iubire intr-o tara indepartata | Maitreyi 0,575 · Enigma Otiliei 0,493 · Ion 0,476 |
| un detectiv si o crima intr-un tren | Crima din Orient Express 0,675 · Cainele din Baskerville 0,484 · Crima si pedeapsa 0,476 |
| cum scriu cod usor de intretinut | Clean Code 0,563 · The Pragmatic Programmer 0,352 · Domain-Driven Design 0,322 |
| o carte despre un razboi si un soldat (aleasa de mine) | Arme, germeni si otel 0,429 · Ultima noapte de dragoste, intaia noapte de razboi 0,419 · Sapiens 0,410 |

| Intrebare | Raspuns |
|-|-|
| Exemplu de carte gasita fara cuvinte comune | „o iubire intr-o tara indepartata” → **Maitreyi** („un inginer european in India traieste o iubire”). „Tara indepartata” nu apare, dar modelul a legat-o de India. |
| Rezultat surprinzator? | La cautarea mea despre razboi si soldat, pe primul loc a iesit „Arme, germeni si otel”, o carte de istorie, probabil din cauza cuvantului „arme”. Padurea spanzuratilor, care chiar e despre un ofiter in razboi, nu a intrat in primele 3. |

## Misiunea 2 · Regulamentul devine tool MCP

* La pornire: `RAG: 13 fragmente din regulament (sectiuni) + 40 carti indexate.`
* Fisierele de pe Moodle aveau linia `ChunkMode` comentata in `DocumentIndex.cs`, asa ca nu compila. Am decomentat-o (valoarea initiala `"sectiuni"`) si am adaugat `using`-urile pentru namespace-urile proiectului.

| # | Intrebarea | Optiunea 3: primul fragment · scor | Optiunea 2: tool-uri · sectiunea · corect? |
|-|-|-|-|
| 1 | Cate carti poate imprumuta un student la master? | §3 Numarul de carti imprumutate · 0,772 | `search_documents` · nu a citat sectiunea · **Da**, 5 carti (§3). |
| 2 | Cat platesc daca intarzii 10 zile? | §12 Sugestii de achizitie · 0,464 (§6 e abia pe locul 3, 0,442) | `search_documents(top=1)` · nu a citat · **Partial**: 1 leu pe zi e corect (§6), dar nu a calculat 10 lei. |
| 3 | Pot prelungi imprumutul? | §4 Durata imprumutului si prelungirea · 0,710 | `search_documents` · nu a citat · **Partial**: da, daca nu e rezervata, dar a uitat ca e o singura data, cu inca 14 zile. |
| 4 | Femeie care isi cauta sotul disparut. Cat costa? | carte: Fata cu un tatuaj de dragon · 0,480 (Baltagul pe locul 3, 0,456) | `search_documents(source=carte)` · **Nu**: a dat 3 carti fara sa aleaga Baltagul si a zis ca nu stie pretul. Corect: Baltagul, 32 lei. |
| 5 | Programul in sesiune? | §9 Programul bibliotecii · 0,652 | `search_documents` · nu a citat · **Partial**: 22:00 in sesiune e corect, dar a zis ca sambata e inchis (de fapt e 9–14) si a adaugat ceva despre sugestii de achizitie. |
| 6 | Pot veni cu cainele in sala de lectura? | §8 Sala de lectura · 0,674 | `search_documents` · nu a citat · **Da**: nu, cu exceptia cainilor-ghid si de asistenta (§8). |

| Intrebare | Raspuns |
|-|-|
| La 4, ce tool-uri si in ce ordine? | Doar `search_documents` cu `source=carte`. Nu a mai apelat `search_books` sau `get_book_details`, desi de acolo ar fi aflat pretul. |
| La 6, ce fragmente in optiunea 3? Ce a raspuns? | §8 Sala de lectura (0,674), §1 Cine poate imprumuta (0,464), §3 Numarul de carti (0,429). A raspuns corect, din §8. |
| Un raspuns care nu se sprijina pe fragmente? | **Da, la 5:** „sambata inchis” nu scrie nicaieri, regulamentul zice sambata 9–14. Si la 4 a zis ca nu stie pretul, in loc sa-l caute cu alt tool. |

## Misiunea 3 · Ce schimba calitatea cautarii

### Partea A · top-k (intrebarea 2, regulament)

| | Locul si scorul fragmentului 6 | Fragmente fara legatura |
|-|-|-|
| top = 1 | nu apare | §12 Sugestii de achizitie |
| top = 3 | locul 3, 0,442 | §12 Sugestii de achizitie (§4 e partial legat) |
| top = 8 | locul 3, 0,442 | §12, §1, §3, §10, §5 |

### Partea B · Chunking

| ChunkMode | Fragmente | Intrebarea 2: primul fragment | Intrebarea 5: primul fragment |
|-|-|-|-|
| sectiuni (initial) | 13 | §12 Sugestii de achizitie · 0,464 | §9 Programul bibliotecii · 0,652 |
| document | 1 | tot regulamentul · 0,439 | tot regulamentul · 0,548 |
| propozitii | 40 | §10 Acces online (2) · 0,505 | §9 Programul (1), propozitia cu semestrul · 0,700 |

### Partea C · Modelul de embeddings

| Intrebarea | embeddinggemma | nomic-embed-text |
|-|-|-|
| Cate carti poate imprumuta un student la master? | §3 Numarul de carti · 0,772 (corect) | §1 Cine poate imprumuta · 0,816 (gresit) |
| Cat platesc daca intarzii 10 zile? | §12 Sugestii de achizitie · 0,464 (gresit) | §6 Intarzieri si penalitati · 0,781 (corect) |
| How many books can a master student borrow? | §3 Numarul de carti · 0,659 (corect) | §1 Cine poate imprumuta · 0,501 (gresit) |

### Partea D · Intrebare fara raspuns

| Intrebarea | Primele 3 fragmente | Ce a raspuns agentul? |
|-|-|-|
| Ce meniu are cantina azi? | §8 Sala de lectura 0,264 · Carte noua, fara evaluari 0,254 · §9 Programul 0,252 | „Nu am gasit informatii despre meniul cantinei.” Corect, nu a inventat. Cautarea intoarce mereu ceva, dar cu scoruri mici (~0,26, fata de 0,65–0,77 cand chiar gaseste). |

**Concluzia mea:** cel mai mult a schimbat **modelul de embeddings**. Acelasi regulament si aceleasi intrebari au dat alt prim fragment la toate 3: nomic-embed-text a gasit penalitatile, dar a gresit numarul de carti si nu a inteles engleza, iar embeddinggemma invers. Pe locul doi e **chunking-ul**: cu tot documentul intr-un singur fragment cautarea nu mai alege nimic, iar pe propozitii raspunsul se poate rupe in bucati.
