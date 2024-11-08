# Protocollo di trasmissione
Il seguente documento spiega il protocollo di comunicazione tra MCU e APP   

## NOTA
Dove **NON** specificato ogni stringa è in formato UNICODE cioè ogni carattere è trasmesso con 2 byte in formato LITTLE-ENDIAN   
### Esempio
- `Hello World` -> hex -> 48 65 6C 6C 6F 20 57 6F 72 6C 64
  - Payload = `48 00 65 00 6C 00 6C 00 6F 00 20 00 57 00 6F 00 72 00 6C 00 64 00`
  
- `正文` -> hex -> 6B63 6587
  - Payload = `63 6B 87 65`

- `О себе` -> hex -> 041E 20 0441 0435 0431 0435
  - Payload = `1E 04 20 00 41 04 35 04 31 04 35 04`

# Comandi

## Ping 
**Descrizione**:   
Il comando è trasmesso dall'MCU in modo periodico ogni 500ms ed è utilizzato per determinare se l'APP è connessa o meno

**Formato**:    
`ping\r\n` -> hex -> 70 69 6E 67 0D 0A
  - Payload = `70 00 69 00 6E 00 67 00 0D 00 0A 00`

**Risposta in formato ASCII**:   
`pong\r\n` -> hex -> 70 6F 6E 67 0A 0D 0A
  - Payload = `70 6F 6E 67 0A 0D 0A`

## File 
**Descrizione**:   
Il comando è trasmesso dall'MCU su richiesta dell'operatore per eseguire la stampa di un ticket

**Formato**:    
`START OF FILE\r\n` -> hex -> 53 54 41 52 54 20 4F 46 20 46 49 4C 45 0D 0A
  - Payload = `53 00 54 00 41 00 52 00 54 00 20 00 4F 00 46 00 20 00 46 00 49 00 4C 00 45 00 0D 00 0A 00`

[...] CORPO DEL FILE [...]   
[...] CRC32 [...]   
[...] \r\n [...]   

`END OF FILE\r\n` -> hex -> 45 4E 44 20 4F 46 20 46 49 4C 45 0D 0A
  - Payload = `45 00 4E 00 44 00 20 00 4F 00 46 00 20 00 46 00 49 00 4C 00 45 00 0D 00 0A 00`

**Risposta in formato ASCII**:   
`OK\r\n` -> hex -> 4F 4B 0D 0A -> Se il CRC32 calcolato e quello ricevuto coincidono
  - Payload = `4F 4B 0D 0A`

`ERROR\r\n` -> hex -> 45 52 52 4F 52 -> Se il CRC32 calcolato e quello ricevuto NON coincidono
  - Payload = `45 52 52 4F 52 0D 0A`


### CRC32
Il CRC32 è calcolato da questa funzione utilizzando il polinomio 0x04C11DB7, 0xFFFFFFFF come valore iniziale e 0xFFFFFFFF come valore di XOR finale   
Il valore ottenuto è convertito in stringa e trasmesso come UNICODE seguito da **\r\n**  

**Esempio**   
crc32 = FF54C264 = `FF 00 54 00 C2 00 64 00 0D 00 0A 00`

```C
#define CRC32_POLY      0x04C11DB7
#define CRC32_INIT      0xFFFFFFFF
#define CRC32_FINAL_XOR 0xFFFFFFFF

uint32_t crc32(uint8_t *p_data, uint32_t len, uint32_t init, uint32_t final_xor) 
{
  uint16_t i = 0;
  uint32_t val;
  uint32_t crc = init;
  
  while(len--) 
  {
    val = (crc ^ *p_data++) & 0xFF;
    
    for(i = 0; i < 8; i++)
      val = (val & 1) ? (val>>1)^CRC32_POLY : val>>1;
    
    crc = val ^ crc >> 8;
  }
  
  return crc ^ final_xor;
}

int16_t main(void)
{
  const char* msg = "Hello World";
  char* unicode_msg = Convert_to_unicode(msg);
  uint32_t crc = CRC32_INIT;

  if(unicode_msg != NULL)
  {
    uint32_t len = Unicode_strlen(unicode_msg);
    crc = crc32(unicode_msg, len, CRC32_INIT, CRC32_FINAL_XOR);

    ...
  }

  return 0;
}
```
