# -*- coding: utf-8 -*-
"""Genera el PDF de ejemplo (guia de fracciones) para el video de miTutorIA.
El ejercicio 3 es 1/2 + 1/3, para que coincida con el guion del video."""
from fpdf import FPDF

# Paleta miTutorIA
SAGE = (74, 103, 65)      # verde
RUST = (193, 68, 14)      # terracota #C1440E
INK = (40, 40, 40)
SOFT = (120, 120, 120)
BOX_BG = (244, 241, 232)

pdf = FPDF(format="A4")
pdf.set_auto_page_break(auto=True, margin=18)
pdf.add_page()
pdf.set_margins(20, 18, 20)

# ---- Encabezado ----
pdf.set_font("Helvetica", "B", 20)
pdf.set_text_color(*SAGE)
pdf.cell(0, 10, "Guia de practica - Fracciones", ln=True)

pdf.set_font("Helvetica", "", 12)
pdf.set_text_color(*RUST)
pdf.cell(0, 8, "Suma y resta de fracciones", ln=True)

pdf.set_draw_color(*SAGE)
pdf.set_line_width(0.6)
y = pdf.get_y() + 2
pdf.line(20, y, 190, y)
pdf.ln(8)

# ---- Recordatorio ----
pdf.set_fill_color(*BOX_BG)
pdf.set_text_color(*INK)
box_top = pdf.get_y()
pdf.set_font("Helvetica", "B", 11)
pdf.set_xy(20, box_top + 3)
pdf.cell(0, 6, "Para acordarte", ln=True)
pdf.set_font("Helvetica", "", 10.5)
pdf.set_x(20)
pdf.multi_cell(
    170, 6,
    "Para sumar o restar fracciones, los denominadores (el numero de abajo) tienen "
    "que ser iguales. Si no lo son, buscamos un denominador comun y convertimos "
    "cada fraccion antes de operar. Solo se suman o restan los numeradores; el "
    "denominador comun se mantiene.",
)
box_bottom = pdf.get_y() + 3
pdf.set_draw_color(225, 220, 208)
pdf.set_line_width(0.3)
pdf.rect(18, box_top, 174, box_bottom - box_top)
pdf.ln(8)

# ---- Consigna ----
pdf.set_font("Helvetica", "B", 12)
pdf.set_text_color(*SAGE)
pdf.cell(0, 8, "Resolve los siguientes ejercicios", ln=True)
pdf.set_font("Helvetica", "", 9.5)
pdf.set_text_color(*SOFT)
pdf.cell(0, 5, "Mostra el procedimiento, no solo el resultado.", ln=True)
pdf.ln(4)

ejercicios = [
    "1)   1/4  +  1/4  =",
    "2)   2/5  +  1/5  =",
    "3)   1/2  +  1/3  =        (este lo trabajamos juntos en el video)",
    "4)   3/4  -  1/2  =",
    "5)   2/3  +  1/6  =",
    "6)   5/8  -  1/4  =",
]

pdf.set_text_color(*INK)
for ej in ejercicios:
    pdf.set_font("Helvetica", "B", 13)
    pdf.cell(0, 11, ej, ln=True)
    pdf.set_draw_color(210, 210, 210)
    pdf.set_line_width(0.2)
    ly = pdf.get_y() + 1
    pdf.line(20, ly, 190, ly)
    pdf.ln(5)

# ---- Pie ----
pdf.set_y(-22)
pdf.set_font("Helvetica", "I", 9)
pdf.set_text_color(*SOFT)
pdf.cell(0, 6, "miTutorIA - material de ejemplo. Subi esta hoja en el aula y pedile ayuda al tutor.", ln=True, align="C")

out = r"C:\Users\Horacio\source\repos\MiTutorIA\marketing\guia-fracciones-ejemplo.pdf"
pdf.output(out)
print("OK ->", out)
