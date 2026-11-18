# Registro delle versioni

[English](CHANGELOG.en.md)

## 1.1b (beta) – 18/11/2026

- **Interfaccia in inglese**: tutti i testi del programma e dell'installer sono tradotti. La lingua si sceglie nelle Preferenze (si applica al riavvio); al primo avvio segue quella di Windows.
- Le traduzioni stanno in un file di testo (`lang\en.txt`): per aggiungere altre lingue non serve toccare il codice dell'interfaccia.
- L'eseguibile si chiama ora `XumerTool.exe`; l'installer toglie il vecchio `XumTool.exe`.
- Nell'installazione c'è anche `README.txt` in inglese.
- Icona aggiornata: sull'etichetta ora c'è scritto "xumerTool".

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
