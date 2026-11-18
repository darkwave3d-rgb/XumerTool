# Registro delle versioni

[English](CHANGELOG.en.md)

## 1.2b (beta) – 18/11/2026

- **Solo versione portatile**: l'installer è messo da parte per ora. Lo zip si estrae dove si vuole; `LEGGIMI.txt` e `README.txt` spiegano come partire e come toglierlo (basta eliminare la cartella).
- **Nuova barra degli strumenti a nastro**: ogni sezione ha il titolo al centro e ogni comando è un tassello cliccabile con il suo nome e l'icona; quando la finestra si stringe i nomi si accorciano. "formatta" ha il triangolo di pericolo.
- **Copia veloce (sperimentale)**: in "crea immagine" il tassello "veloce" (icona del disco con un fulmine giallo) avvia la copia veloce, con il protocollo S2 di OpenCBM: un disco in circa un minuto invece di dieci. Legge soltanto; se qualcosa va storto rifà la copia con il metodo normale. Provata con un 1571 e immagini D64.
- **Scheda Internet Archive**: collegamento al tuo account archive.org (con email e password, che non vengono salvate, oppure con le chiavi S3). Le chiavi restano sul PC, cifrate. Il caricamento delle immagini arriverà nella prossima versione.
- **Legenda dei messaggi del drive**: nella scheda Comandi DOS ("Messaggi di errore") tutti i codici del DOS con il significato e cosa fare; nel registro la spiegazione compare da sola sotto ogni errore.
- **Provato con il 1541**: lettura e formattazione funzionano.
- **Mappa del disco**: durante "Crea immagine", "Scrivi immagine" e "Disco in VICE" lo schermo blu mostra tracce e settori che si colorano man mano (fatti, in corso, con errori). Le D71 mostrano le due facce affiancate.
- **Drive nell'intestazione**: un piccolo drive disegnato con il LED verde di alimentazione e quello rosso che sfarfalla durante il lavoro e lampeggia dopo un errore.
- **Icone dei tipi di file** (PRG, SEQ, USR, REL, DEL) in stile pixel nella directory.
- **Barra di avanzamento a blocchi** in stile PETSCII.
- Il salvaschermo non parte più durante un'operazione in corso.
- Corretto: dopo il collegamento o la lettura della directory i pulsanti a volte restavano disattivati (si riattivavano solo cambiando tema).
- Apertura più rapida: la ricerca di VICE avviene in sottofondo.
- Lettura delle immagini più sicura: un settore arrivato incompleto dà errore invece di finire nell'immagine.
- Chiudere il programma durante un'operazione non lo fa più bloccare.

## 1.1b (beta) – 18/11/2026

- **Interfaccia in inglese**: tutti i testi del programma sono tradotti. La lingua si sceglie nelle Preferenze (si applica al riavvio); al primo avvio segue quella di Windows.
- Le traduzioni stanno in un file di testo (`lang\en.txt`): per aggiungere altre lingue non serve toccare il codice dell'interfaccia.
- **Versione portatile**: oltre all'installer, uno zip da estrarre e avviare senza installare nulla.
- L'eseguibile si chiama ora `XumerTool.exe`.
- Icona aggiornata: sull'etichetta ora c'è scritto "xumerTool"; si carica più in fretta nella barra di Windows.

## 1.0b (beta) – 18/11/2026

Prima versione pubblica, in prova.

**Drive e dischi** (sviluppato e provato con xum1541 su Arduino Pro Micro e un Commodore 1571):
- collegamento automatico all'adattatore, reset del bus, scelta del drive 8–11
- directory, lettura e scrittura di file PRG/SEQ/P00, rinomina, cancellazione, formattazione, validazione
- immagini disco D64, D71 (1571 doppia faccia) e D81, in lettura e in scrittura
- comandi DOS con elenco dei comandi pronti e lettura dello stato del drive

**Interfaccia**
- 10 temi: Chiaro, Commodore, Rosé Pine, Scuro, Tokyo Night, Catppuccin, Everforest, Gruvbox, Nord, Lunar Tear, scelti da una tendina con anteprima
- barre che si adattano alla larghezza della finestra (testi brevi, poi solo icone con suggerimento)
- 10 caratteri retro per lo schermo della directory
- avvio dei programmi e dei dischi nell'emulatore VICE
- giochi originali (Floppy Runner, Assalto PETSCII) e salvaschermo PETSCII
- installer per utente, senza diritti di amministratore

**Ancora da provare sull'hardware**
- copia veloce (protocollo S2 di OpenCBM)
- scrittura del firmware xum1541 sul Pro Micro
- drive 1541 e 1581 (immagini D81): il programma li gestisce ma è stato provato solo con un 1571
