import random
import time
from typing import List, Union
from PyFlow import *
from scipy import stats
import sys

       
def main(priorities, inspections, printOutput=0) -> List[Union[SimClock, Sink]]:
    
    clock = SimClock.get_instance()

    # chapa_item = Item(0, name = "Chapa", model_item=True, labels={"inspeccionOn":1})
    chapa_item = Item(0, name = "Chapa", model_item=True)
    refuerzo_item = Item(0, name="previa", model_item=True)



    # Direct from the excel File
    # source_chapas = ScheduleSource("SourceChapas", clock, file_name="CEMI_chapas.xlsx", model_item=chapa_item, sheet_name="MBOM")
    # source_refuerzos = ScheduleSource("SourceRefuerzos", clock, file_name="CEMI_refuerzos.xlsx", model_item=refuerzo_item, sheet_name="MBOM")
    
    # For optimization
    chapas = SeqOptTools.read_excel_to_dict("Data\\CEMI_chapas.xlsx", "MBOM")
    SeqOptTools.add_labels_to_dict(chapas, "inspeccionOn", inspections)
    chapas_reordered = SeqOptTools.transform_sequence(chapas, priorities)

    refuerzos = SeqOptTools.read_excel_to_dict("Data\\CEMI_refuerzos.xlsx", "MBOM")
    refuerzos_reordered = SeqOptTools.transform_sequence(refuerzos, priorities)

    source_chapas = ScheduleSource("SourceChapas", clock, model_item=chapa_item, data_dict=chapas_reordered)
    source_refuerzos = ScheduleSource("SourceRefuerzos", clock, model_item=refuerzo_item, data_dict=refuerzos_reordered)
    buffer_chapas = ItemsQueue(1000, "QueueChapas", clock)
    buffer_refuerzos = ItemsQueue(1000, "QueueRefuerzos", clock)

    welding = Combiner([1], "item.get_label_value('tSoldadura') + item.get_label_value('tInspeccion')* item.get_label_value('inspeccionOn')", 
                        "Welding", clock, pull_mode=SingleLabelStrategy("Referencia"), update_requirements=True, 
                        update_labels=["nRefuerzos"])
    
    sink = Sink("Sink", clock)
    
    source_chapas.connect([buffer_chapas])
    source_refuerzos.connect([buffer_refuerzos])
    buffer_chapas.connect([welding])
    buffer_refuerzos.connect([welding.get_component_input(0)])
    welding.connect([sink])

    clock.initialize()
    max_sim_time = 100000
    clock.advance_clock(max_sim_time)

    if printOutput:
        print(f"\nSimulation Time: {clock.get_simulation_time()}")
        print(f"Items processed: {sink.get_stats_collector().get_var_input_value()}") 

        print(f"Buffer Avg Staytime: {buffer_refuerzos.get_stats_collector().get_var_staytime_average()}")
        print(f"Buffer Avg Queue Length: {buffer_refuerzos.get_stats_collector().get_var_content_max()}")

    return [clock, sink]

if __name__ == "__main__":
    priorities = [50, 36, 74, 42, 45, 71, 51, 54, 4, 8, 35, 13, 10, 19, 9, 59, 49, 58, 23, 64, 76, 69, 60, 5, 21, 15, 89, 24, 30, 85, 68, 67, 34, 79, 28, 29, 80, 73, 37, 11, 48, 12, 22, 53, 81, 70, 47, 44, 38, 78, 90, 88, 77, 72, 57, 43, 20, 66, 18, 1, 6, 61, 33, 3, 65, 63, 83, 46, 55, 41, 31, 2, 62, 16, 52, 25, 84, 87, 75, 56, 27, 86, 82, 32, 14, 26, 17, 39, 7, 40]
    inspections = [1, 0, 0, 1, 0, 0, 1, 0, 0, 1, 1, 1, 1, 1, 0, 0, 1, 0, 1, 1, 0, 1, 1, 0, 1, 0, 1, 0, 0, 1, 1, 0, 1, 0, 1, 0, 1, 1, 0, 1, 1, 1, 1, 0, 0, 1, 1, 1, 1, 1, 0, 0, 1, 1, 1, 1, 1, 0, 0, 1, 0, 1, 1, 0, 1, 0, 1, 1, 1, 0, 1, 1, 1, 0, 1, 1, 0, 1, 1, 1, 0, 0, 1, 1, 1, 1, 0, 1, 0, 1]

    main(priorities, inspections, 1)  # Llamar al método main