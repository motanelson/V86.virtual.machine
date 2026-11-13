; ==========================================
; Classic DOS Hello World
; mov ah, 9 followed by int 0x21 to print
; int 20 to terminate
; ==========================================
org 100h

start:
    mov ah, 9            ; DOS print string function
    mov dx, offset msg   ; Pointer to $-terminated string in DS:DX
    int 0x21             ; Call DOS interrupt

    int 20               ; Terminate execution

msg:
    db 'Hello, x86 Virtual Machine!', 0Dh, 0Ah
    db 'Printed via INT 0x21 (AH=09h)$'
