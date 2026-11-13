; ==========================================
; Direct Video RAM: writes directly to 0xB800
; ==========================================
org 100h

start:
    mov ax, 0B800h       ; VGA text mode memory
    mov es, ax
    mov di, 0

    mov byte [es:di], 'H'
    mov byte [es:di+1], 03h

    mov byte [es:di+2], 'e'
    mov byte [es:di+3], 1Eh

    mov byte [es:di+4], 'l'
    mov byte [es:di+5], 2Eh

    mov byte [es:di+6], 'l'
    mov byte [es:di+7], 3Eh

    mov byte [es:di+8], 'o'
    mov byte [es:di+9], 4Fh

    int 20
