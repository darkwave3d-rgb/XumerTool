# Cose da fare

**Italiano** · [English](TODO.md)

## Da verificare sull'hardware (prima di tutto)

- Scrittura del firmware sul Pro Micro (scheda Adattatore → Installa firmware). Primo test consigliato: reinstallare xum1541 v08, con il cavo IEC scollegato.
- Download del firmware e di Zadig da GitHub (scheda Adattatore → Strumenti necessari).
- Installer completo (`bin\XUMER-TOOL-Setup.exe`): collegamenti nel menu Start e sul Desktop, voce in "App installate", disinstallazione. Finora provato solo in una cartella di prova.
- Giochi Floppy Runner (nuova grafica) e Assalto PETSCII, salvaschermo: provati solo con screenshot statici.
- Preferenze: tendina dei temi (mouse e frecce), cambio di tema e carattere, poi Salva.
- Finestra stretta trascinata a mano: le barre si compattano, i suggerimenti compaiono sulle icone.
- Icona nuova nella barra di Windows e nel menu Start, dopo la reinstallazione.

## Lavori grandi, in ordine

1. **Copia veloce (protocollo S2)**
   - Il codice c'è già (`src/FastCopy.cs`, `src/Asm6502.cs`, `drivecode/*.a65`) ma non è mai stato provato sul drive vero.
   - Provare l'assemblatore e la lettura veloce con un disco inserito, poi la scrittura su un disco da sacrificare, infine aggiungere l'opzione nell'interfaccia.
   - Risultato atteso: un disco intero in meno di un minuto, invece di circa 10.
2. **Il drive come unità di Windows (es. Z:)**
   - Il disco nel 1571 compare in Esplora risorse come un'unità normale.
   - Serve il driver gratuito WinFsp; XUMER TOOL fa da ponte verso l'adattatore.
   - Tenere in memoria directory e file già letti; leggere dal drive solo quando un file viene aperto davvero.
   - In più: montare come unità anche un'immagine D64/D71.
3. **RFIEC: il PC come drive per il C64** (facoltativo)
   - Serve solo con il firmware RFIEC di RetroFixer: il C64 carica e salva i file sul PC.
   - I sorgenti di RFIEC sono GPL-3: riscrivere dal protocollo, senza copiare codice, per restare in licenza MIT.

## Idee proposte (da scegliere)

I miei consigli: visualizzatore D64, mappa del disco animata, animazione del drive con il LED.

### Funzioni

- **Visualizzatore di immagini D64/D71/D81**: aprire un'immagine dal PC e vederne la directory nello schermo blu; estrarre file, avviarli in VICE, scriverla su disco. Funziona senza drive.
- **Anteprima dei file**: listato BASIC decodificato, testo dei SEQ, vista esadecimale.
- **Copia disco → disco** in un solo passaggio.
- **Mappa del disco (BAM)**: griglia di tracce e settori colorati (liberi, occupati, con errori), che si riempie durante la lettura.
- **Archivio dei dischetti**: elenco delle immagini create con ricerca ("in quale dischetto c'era…?").
- **Verifica dopo la scrittura** di un'immagine.
