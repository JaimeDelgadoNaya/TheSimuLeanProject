import pandas as pd

class SeqOptTools:
    """
    A utility class for sequence optimization tasks, including reading Excel files
    and converting their content into Python dictionaries for further processing.
    """
    
    @staticmethod
    def read_excel_to_dict(file_path: str, sheet_name: str = None) -> dict:
        """
        Reads an Excel data file and converts its content into a dictionary.

        Args:
            file_path (str): The path to the Excel file to read.
            sheet_name (str, optional): The name of the sheet to read. Defaults to the first sheet.

        Returns:
            dict: A dictionary where keys are column headers and values are lists of column data.

        Raises:
            FileNotFoundError: If the specified file does not exist.
            ValueError: If the file cannot be read as an Excel file.
        """
        try:
            # Read the Excel file into a DataFrame
            df = pd.read_excel(file_path, sheet_name=sheet_name)
            
            # Convert the DataFrame to a dictionary
            data_dict = df.to_dict(orient='list')
            
            return data_dict
        except FileNotFoundError as e:
            raise FileNotFoundError(f"File not found: {file_path}") from e
        except ValueError as e:
            raise ValueError(f"Error reading the Excel file: {file_path}") from e
        
    @staticmethod
    def transform_sequence(data_dict: dict, priorities: list) -> dict:
        """
        Reorders the values within each key of a dictionary based on a list of priorities.
        The priorities must start at number 1 and have a length equal to the length of the dictionary.

        Args:
            data_dict (dict): The dictionary to reorder.
            priorities (list): The list of priorities to reorder the values by.

        Returns:
            dict: The dictionary with reordered values.

        Raises:
            ValueError: If the priorities list does not start at 1 or its length does not match the length of the dictionary.
        """
        # Validate that all values in the dictionary match the length of priorities
        for key, values in data_dict.items():
            if len(values) != len(priorities):
                raise ValueError(f"Length mismatch for key '{key}': values have length {len(values)}, priorities have length {len(priorities)}")

        # Create the reordered dictionary
        reordered_dict = {}

        for key, values in data_dict.items():
            reordered_values = [None] * len(priorities)  # Initialize with placeholders
            for current_index, priority_position in enumerate(priorities):
                # Place the current value into the new position
                if isinstance(priority_position, int) and 1 <= priority_position <= len(priorities):
                    reordered_values[priority_position - 1] = values[current_index]  # Adjust for 1-based priority
                else:
                    raise IndexError(f"Priority position {priority_position} is out of range for priorities list")
            reordered_dict[key] = reordered_values

        return reordered_dict


    @staticmethod
    def add_labels_to_dict(data_dict: dict, new_label_name: str, new_label_values: list) -> dict:
        """
        Adds a new label and its corresponding values to the dictionary. If the label already exists, it overwrites their values.

        Args:
            data_dict (dict): The data dictionary to update.
            new_label_name (str): The new label name to add.
            new_label_values (list): The list of values for the new label.

        Returns:
            dict: The updated dictionary.
        """
        # if new_label_name in data_dict:
        #     raise ValueError(f"The label name '{new_label_name}' already exists in the dictionary.")
        
        # Check if the length of new_entries matches the length of other entries
        if data_dict:
            first_key = next(iter(data_dict))
            if len(new_label_values) != len(data_dict[first_key]):
                raise ValueError(f"The length of new_label_values ({len(new_label_values)}) does not match the length of other entries ({len(data_dict[first_key])}).")
        
        data_dict[new_label_name] = new_label_values
        return data_dict