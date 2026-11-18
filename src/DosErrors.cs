// Legenda dei messaggi del canale comandi (DOS dei drive 1541, 1570/1571 e 1581):
// numero, testo del drive, cosa significa e cosa fare. Usata nella scheda Comandi DOS e nel registro.
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace XumerTool
{
    class DosError
    {
        public int Code;
        public string Text;      // come lo scrive il drive (non si traduce)
        public string Meaning;   // cosa significa
        public string Fix;       // cosa fare
        public bool Info;        // messaggio informativo, non un errore
    }

    static class DosErrors
    {
        static DosError E(int code, string text, string meaning, string fix, bool info = false)
        {
            return new DosError { Code = code, Text = text, Meaning = meaning, Fix = fix, Info = info };
        }

        public static readonly DosError[] All =
        {
            E(0, "OK", Lang.T("Tutto a posto: l'ultima operazione è riuscita."), Lang.T("Niente da fare."), true),
            E(1, "FILES SCRATCHED", Lang.T("File cancellati: il primo numero dopo il messaggio dice quanti."), Lang.T("Niente da fare: è la conferma di una cancellazione."), true),
            E(2, "SELECTED PARTITION", Lang.T("Partizione selezionata (solo 1581)."), Lang.T("Niente da fare."), true),
            E(20, "READ ERROR", Lang.T("Intestazione del blocco non trovata: il settore non si legge."), Lang.T("Riprova; pulisci la testina; se succede con tutti i dischi il drive va riallineato. Con Crea immagine salvi il resto del disco."), false),
            E(21, "READ ERROR", Lang.T("Nessun segnale di sincronismo: disco non formattato, assente o inserito male."), Lang.T("Controlla il disco e chiudi bene lo sportello; un disco nuovo va prima formattato."), false),
            E(22, "READ ERROR", Lang.T("Blocco dati non trovato: il settore è rovinato."), Lang.T("Riprova più volte; con Crea immagine salvi i settori buoni (l'errore resta segnato nell'immagine)."), false),
            E(23, "READ ERROR", Lang.T("Dati del settore corrotti (checksum errato): disco danneggiato o sporco."), Lang.T("Riprova e pulisci la testina; salva il disco con Crea immagine, poi copia i file su un disco nuovo."), false),
            E(24, "READ ERROR", Lang.T("Errore di decodifica dei dati letti: superficie rovinata o problema del drive."), Lang.T("Prova un altro disco: se l'errore resta, il drive va controllato."), false),
            E(25, "WRITE ERROR", Lang.T("Verifica dopo la scrittura fallita: i dati scritti non corrispondono."), Lang.T("Il disco è consumato o difettoso: usane un altro."), false),
            E(26, "WRITE PROTECT ON", Lang.T("Disco protetto dalla scrittura."), Lang.T("Togli l'adesivo dalla tacca laterale (o usa un disco con la tacca libera)."), false),
            E(27, "READ ERROR", Lang.T("Intestazione del settore corrotta (checksum errato)."), Lang.T("Disco danneggiato: salva il possibile con Crea immagine."), false),
            E(28, "WRITE ERROR", Lang.T("Blocco troppo lungo in scrittura: disco rovinato o velocità del motore sbagliata."), Lang.T("Prova un altro disco; se succede sempre, il drive va regolato."), false),
            E(29, "DISK ID MISMATCH", Lang.T("L'ID del disco non è quello atteso: disco cambiato senza inizializzarlo, o intestazione rovinata."), Lang.T("Rileggi la Directory (o invia I0) e riprova."), false),
            E(30, "SYNTAX ERROR", Lang.T("Comando non capito dal drive."), Lang.T("Controlla come è scritto il comando (vedi Comandi pronti)."), false),
            E(31, "SYNTAX ERROR", Lang.T("Comando sconosciuto."), Lang.T("Controlla la prima lettera del comando."), false),
            E(32, "SYNTAX ERROR", Lang.T("Comando troppo lungo (oltre 58 caratteri)."), Lang.T("Accorcia il comando o il nome del file."), false),
            E(33, "SYNTAX ERROR", Lang.T("Nome di file non valido (es. * o ? dove non sono ammessi)."), Lang.T("Usa i caratteri jolly solo per directory, cancellazione e caricamento."), false),
            E(34, "SYNTAX ERROR", Lang.T("Nome del file mancante."), Lang.T("Aggiungi il nome dopo i due punti (es. S0:NOME)."), false),
            E(39, "SYNTAX ERROR", Lang.T("Comando non valido per il canale comandi."), Lang.T("Controlla il comando."), false),
            E(50, "RECORD NOT PRESENT", Lang.T("File relativo (REL): record oltre la fine del file."), Lang.T("Normale quando si allunga un file REL; altrimenti controlla il numero del record."), false),
            E(51, "OVERFLOW IN RECORD", Lang.T("File relativo (REL): dati più lunghi del record."), Lang.T("Accorcia i dati o usa record più lunghi."), false),
            E(52, "FILE TOO LARGE", Lang.T("File relativo (REL): il record non sta più sul disco."), Lang.T("Libera spazio o usa un altro disco."), false),
            E(60, "WRITE FILE OPEN", Lang.T("Il file è ancora aperto in scrittura."), Lang.T("Chiudi il file; i file rimasti aperti (segnati con *) si sistemano con Valida."), false),
            E(61, "FILE NOT OPEN", Lang.T("Il file non è aperto."), Lang.T("Apri il file prima di leggerlo o scriverlo; un Reset bus rimette tutto a posto."), false),
            E(62, "FILE NOT FOUND", Lang.T("File non trovato."), Lang.T("Controlla il nome con la Directory (maiuscole, spazi); il jolly * aiuta (es. NOME*)."), false),
            E(63, "FILE EXISTS", Lang.T("Esiste già un file con quel nome."), Lang.T("Scegli un altro nome, oppure cancella o rinomina prima quello vecchio."), false),
            E(64, "FILE TYPE MISMATCH", Lang.T("Il tipo del file (PRG, SEQ…) non è quello richiesto."), Lang.T("Apri il file con il suo tipo, come appare nella directory."), false),
            E(65, "NO BLOCK", Lang.T("Il blocco richiesto è già occupato (comando B-A)."), Lang.T("Il numero dopo il messaggio indica il prossimo blocco libero."), false),
            E(66, "ILLEGAL TRACK AND SECTOR", Lang.T("Traccia o settore inesistenti: comando sbagliato o collegamenti dei file rovinati."), Lang.T("Controlla il comando; se capita leggendo un file, prova Valida."), false),
            E(67, "ILLEGAL SYSTEM T OR S", Lang.T("Traccia o settore di sistema non validi."), Lang.T("Disco danneggiato o comando sbagliato: prova Valida."), false),
            E(70, "NO CHANNEL", Lang.T("Nessun canale libero nel drive: troppi file aperti."), Lang.T("Chiudi i file aperti; Reset bus libera tutti i canali."), false),
            E(71, "DIRECTORY ERROR", Lang.T("La mappa dei blocchi (BAM) non corrisponde al disco."), Lang.T("Usa Valida (V0); se il problema resta, salva i file e riformatta."), false),
            E(72, "DISK FULL", Lang.T("Disco pieno, oppure directory piena (144 file su 1541/1571)."), Lang.T("Cancella qualche file o usa un altro disco."), false),
            E(73, "CBM DOS V2.6 / V3.0", Lang.T("Messaggio di accensione o di reset del drive: indica la versione del DOS e il modello."), Lang.T("Niente da fare: non è un errore."), true),
            E(74, "DRIVE NOT READY", Lang.T("Drive non pronto: manca il disco o lo sportello è aperto (per le D71 serve un 1571)."), Lang.T("Inserisci il disco, chiudi lo sportello e riprova."), false),
            E(75, "FORMAT ERROR", Lang.T("Formattazione non riuscita (1571/1581)."), Lang.T("Riprova con un altro disco."), false),
            E(76, "CONTROLLER ERROR", Lang.T("Errore del controller del drive (1571/1581)."), Lang.T("Spegni e riaccendi il drive; se resta, il drive va controllato."), false),
            E(77, "SELECTED PARTITION ILLEGAL", Lang.T("Partizione non valida (solo 1581)."), Lang.T("Controlla il comando della partizione."), false),
        };

        public static DosError Find(int code)
        {
            foreach (DosError e in All) if (e.Code == code) return e;
            return null;
        }

        // risposta del drive: "62, FILE NOT FOUND,00,00" (anche in mezzo a un messaggio d'errore)
        static readonly Regex Status = new Regex(@"\b(\d{2}),\s?[A-Z][A-Z0-9 .()]*,\d{2},\d{2}\b");

        /// <summary>
        /// Spiegazione per una riga del registro che contiene un errore del drive, oppure null
        /// (messaggi informativi esclusi).
        /// </summary>
        public static DosError ErrorIn(string line)
        {
            Match m = Status.Match(line);
            if (!m.Success) return null;
            DosError e = Find(int.Parse(m.Groups[1].Value));
            return e != null && !e.Info ? e : null;
        }
    }
}
