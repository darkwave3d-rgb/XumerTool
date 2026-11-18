# Cose da fare

**Italiano** · [English](TODO.md)

## Da verificare sull'hardware (prima di tutto)

- Scrittura del firmware sul Pro Micro (scheda Adattatore → Installa firmware). Primo test consigliato: reinstallare xum1541 v08, con il cavo IEC scollegato.
- Download del firmware e di Zadig da GitHub (scheda Adattatore → Strumenti necessari).
- Copia veloce con immagini D71 e con il 1541.
- Mappa del disco e LED del drive durante una copia lunga.
- Giochi Floppy Runner (nuova grafica) e Assalto PETSCII, salvaschermo: provati solo con screenshot statici.
- Preferenze: tendina dei temi (mouse e frecce), cambio di tema e carattere, poi Salva.
- Finestra stretta trascinata a mano: le barre si compattano, i suggerimenti compaiono sulle icone.
- Icona nuova nella barra di Windows.

## Lavori grandi, in ordine

1. **Copia veloce (protocollo S2)** – lettura D64 provata sul 1571 (circa un minuto invece di dieci), nel tassello "veloce" di "crea immagine", con ritorno automatico alla copia normale in caso di errore.
   - Restano da provare: immagini D71 e il drive 1541.
   - Scrittura veloce (`WriteImageFast`): da provare su un disco da sacrificare, poi da aggiungere come tassello "veloce" in "scrivi immagine".
2. **Il drive come unità di Windows (es. Z:)**
   - Il disco nel 1571 compare in Esplora risorse come un'unità normale.
   - Serve il driver gratuito WinFsp; XUMER TOOL fa da ponte verso l'adattatore.
   - Tenere in memoria directory e file già letti; leggere dal drive solo quando un file viene aperto davvero.
   - In più: montare come unità anche un'immagine D64/D71.
3. **RFIEC: il PC come drive per il C64** (facoltativo)
   - Serve solo con il firmware RFIEC di RetroFixer: il C64 carica e salva i file sul PC.
   - I sorgenti di RFIEC sono GPL-3: riscrivere dal protocollo, senza copiare codice, per restare in licenza MIT.

## Idee proposte (da scegliere)

I miei consigli: visualizzatore D64, anteprima dei file.

### Funzioni

- **Internet Archive**: la scheda Internet Archive collega l'app a un account di archive.org; il prossimo passo è caricare lì le immagini disco direttamente dall'app.
- **Visualizzatore di immagini D64/D71/D81**: aprire un'immagine dal PC e vederne la directory nello schermo blu; estrarre file, avviarli in VICE, scriverla su disco. Funziona senza drive.
- **Anteprima dei file**: listato BASIC decodificato, testo dei SEQ, vista esadecimale.
- **Copia disco → disco** in un solo passaggio.
- **Mappa della BAM**: blocchi liberi e occupati del disco, senza doverlo copiare.
- **Archivio dei dischetti**: elenco delle immagini create con ricerca ("in quale dischetto c'era…?").
- **Verifica dopo la scrittura** di un'immagine.
