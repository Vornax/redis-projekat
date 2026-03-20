Sistem se sastoji od in-memory baze podataka (Redis), alata za vizuelizaciju baze (RedisInsight), Backend API-ja (.NET 8) i pratećeg Frontenda. Takođe, uključena je i skripta za automatsko inicijalno punjenje baze (Seed).

🛠️ Preduslovi za pokretanje
Da biste pokrenuli projekat, potrebno je da na vašoj mašini imate instalirano:

- Docker Desktop (upaljen i aktivan)
- Git (za preuzimanje repozitorijuma)

🚀 Uputstvo za pokretanje (Korak po korak)

1. Kloniranje repozitorijuma

Otvorite terminal i unesite komande:

git clone <link_do_tvog_github_repozitorijuma>
cd <ime_tvog_foldera>

2. Podešavanje promenljivih okruženja (.env)

Projekat koristi .env fajl za čuvanje konfiguracije i API ključeva.

U root folderu projekta pronađite fajl .env.example.
Napravite kopiju tog fajla i preimenujte je u tačno .env


3. Pokretanje Docker kontejnera

U terminalu, pozicionirani u root folderu (gde je docker-compose.yml), ukucajte:

docker-compose up -d --build

⏳ Sačekajte oko 10-15 sekundi. U pozadini, specifičan redis-seed kontejner čeka da se baza podigne, automatski ubacuje početne "Trending" podatke u Redis, i zatim se gasi.

4. Pristup aplikaciji
Svi servisi su sada aktivni! Otvorite vaš web pretraživač i posetite sledeće linkove:

- 🌐 Glavni Web Sajt (Frontend): http://localhost:5257
- ⚙️ API Dokumentacija (Swagger): http://localhost:5257/swagger (Za testiranje API-ja unesite API_KEY iz .env fajla)
- 📊 Redis GUI (RedisInsight): http://localhost:5540 (Grafički interfejs za pregled in-memory baze)


!!! da bi sve radilo u .env polja API_KEY i Authorization__ApiKey moraju da sadrze istu sifru kao i polje API_KEY u config.js !!!
