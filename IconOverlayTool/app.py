import os
import shutil
from flask import Flask, render_template, request, jsonify
from PIL import Image

app = Flask(__name__)

# Configuration
TARGET_ROOT_DIR = os.path.abspath(os.path.join(os.path.dirname(__file__), '..'))
UPLOAD_FOLDER = os.path.join(os.path.dirname(__file__), 'uploads')
os.makedirs(UPLOAD_FOLDER, exist_ok=True)

@app.route('/')
def index():
    return render_template('index.html')

@app.route('/apply', methods=['POST'])
def apply_overlay():
    try:
        # 1. Get Inputs
        if 'overlay' not in request.files:
            return jsonify({'error': 'No overlay image uploaded'}), 400
        
        overlay_file = request.files['overlay']
        if overlay_file.filename == '':
            return jsonify({'error': 'No selected file'}), 400

        line_type = request.form.get('line_type') # e.g., 'Bus', 'Tram', 'Train', 'Subway', 'Bike'
        if not line_type:
            return jsonify({'error': 'No line type selected'}), 400

        # Save overlay temporarily
        overlay_path = os.path.join(UPLOAD_FOLDER, 'temp_overlay.png')
        overlay_file.save(overlay_path)

        # 2. Process Files
        processed_files = []
        errors = []

        # Keywords to look for in directory names based on Line Type
        # Mapping user selection to potential directory keywords
        keywords = []
        if line_type == 'Bus':
            keywords = ['Bus']
        elif line_type == 'Tram':
            keywords = ['Tram']
        elif line_type == 'Train':
            keywords = ['Train']
        elif line_type == 'Subway':
            keywords = ['Subway']
        elif line_type == 'Bike':
            keywords = ['Bicycle', 'Bike']
        else:
            return jsonify({'error': 'Invalid line type'}), 400

        print(f"Searching for directories containing: {keywords} in {TARGET_ROOT_DIR}")

        for root, dirs, files in os.walk(TARGET_ROOT_DIR):
            # Skip the tool's own directory and hidden folders
            if 'IconOverlayTool' in root or '.git' in root:
                continue

            # Check if current directory matches the line type
            dir_name = os.path.basename(root)
            if any(k.lower() in dir_name.lower() for k in keywords):
                if 'icon.png' in files:
                    icon_path = os.path.join(root, 'icon.png')
                    try:
                        apply_overlay_to_image(icon_path, overlay_path)
                        processed_files.append(icon_path)
                    except Exception as e:
                        errors.append(f"Failed to process {icon_path}: {str(e)}")

        return jsonify({
            'message': 'Processing complete',
            'processed_count': len(processed_files),
            'processed_files': processed_files,
            'errors': errors
        })

    except Exception as e:
        return jsonify({'error': str(e)}), 500

def apply_overlay_to_image(base_image_path, overlay_image_path):
    # Open the base image
    with Image.open(base_image_path) as base_img:
        base_img = base_img.convert("RGBA")
        
        # Open the overlay image
        with Image.open(overlay_image_path) as overlay_img:
            overlay_img = overlay_img.convert("RGBA")
            
            # Resize overlay to a fixed small size (e.g., 64x64 for a 256x256 icon)
            fixed_size = (64, 64)
            overlay_img = overlay_img.resize(fixed_size, Image.Resampling.LANCZOS)

            # Calculate position: Bottom Right
            padding = 10
            x = base_img.width - overlay_img.width - padding
            y = base_img.height - overlay_img.height - padding
            
            # Create a copy to paste onto
            combined = base_img.copy()
            combined.paste(overlay_img, (x, y), overlay_img)
            
            # Save - overwrite original
            # First create a backup if not exists
            backup_path = base_image_path + ".bak"
            if not os.path.exists(backup_path):
                shutil.copy2(base_image_path, backup_path)
            
            combined.save(base_image_path, format="PNG")

if __name__ == '__main__':
    app.run(debug=True)
