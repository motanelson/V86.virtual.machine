; ==========================================
; Arithmetic & Flag Checks
; ==========================================
org 100h

start:
    mov ax, 20
    mov bx, 15
    add ax, bx           ; AX = 35
    cmp ax, 35

    mov ah, 9
    mov dx, offset success
    int 0x21
    int 20

success:
    db 'Calculations verified! AX is 35.$'
