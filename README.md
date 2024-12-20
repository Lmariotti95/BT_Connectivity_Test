# BT_Connectivity_Test

Test per la ricezione di file trasmessi da un MCU verso il PC tramite bluetooth

| Version |
|:-------:|
|  1.1.0  |

## Note di rilascio
Prima versione preliminare, funziona secondo le specifiche aggiornate al **08/11/2024**   

**Funzionalità:**
- Implementa la comunicazione come descritto in: [protocollo di comunicazione](./doc/Protocollo_di_trasmissione.md)
- Si connette in bluetooth con un MCU (1 sola connessione attiva)
- Mantiene attiva la comunicazione rispondendo attivamente a comandi non sollecitati
- Riceve i file .csv
- Converte i file .csv in .pdf dando una formattazione minima
- Gestisce testi UNICODE (russo/cinese)
- Aggiunta la documentazione per la connessione tramite BLE utilizzando la libreria Plugin.BLE [link](./doc/Connessione_BLE.md)

## Bluetooth
- **Produttore**: u-blox
- **Modello**: NINA-B222
- **Configurazione**: Standard e BLE
- **Protocollo bluetooth**: SPS

## App per test
[Android evaluation app for Bluetooth low energy modules](https://github.com/u-blox/Android-u-blox-BLE)   
[iOS evaluation app for Bluetooth low energy modules](https://github.com/u-blox/iOS-u-blox-BLE)

## Configurazione NINA-B222
[Documentazione u-blox](./doc/)

La configurazione del modulo è stata fatta riportando quanto descritto in **UBX-16024251 par. 4.5.16**   

**Problema:** Nonostante la configurazione fosse corretta il service bluetooth **Serial Port** (FIFO & Credit) non veniva gestito.   
**Soluzione:** Abilitare il service manualmente come descritto in **UBX-14044127 par. 6.24.3 esempio in fondo al paragrafo**

### Sequenza di setup finale
```C
Send_at_cmd("AT+CGMI");      // Legge il nome del produttore
Send_at_cmd("AT+CGMM");      // Legge il nome del modello
Send_at_cmd("ATE0");         // Disattiva l'echo sui comandi
Send_at_cmd("AT+UBTLE?");     // Verifica se la modalità BLE è attiva
  // Se non è configurato in modalità BLE
  Send_at_cmd("AT+UBTLE=2");  // Configura la modalità BLE
  Send_at_cmd("AT+UDSC=1,6"); // Abilita il server SPS
  Send_at_cmd("AT&W");        // Salva la configurazione
  Send_at_cmd("AT+CPWROF");   // Esegue un reboot
Send_at_cmd("AT+UBTAD=020A06051218002800110702456E1B926E28F83E744F34F01E9D701"); // Abilita il servizio Serial Port
Send_at_cmd("ATO1");          // Entra in Data Mode
```
