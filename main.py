import requests
import time
import json

# --- USER CONFIGURATION ---
# Replace with your Telegram bot token and chat ID
TELEGRAM_BOT_TOKEN = '8009538506:AAEsIjbLql9pOvsuilXNywDhVF0iL6Bp1GU'
TELEGRAM_CHAT_IDs = ['288792443','71574007']

# IMPORTANT: This authorization token may be temporary.
# If the script stops working, you may need to get a new token.
AUTHORIZATION_TOKEN = 'a3b49511182b41ccb48263dbfd09bc60.clm1md4ybIGz6E8w'

# Define the request payload as a Python dictionary
# This is a direct conversion of the --data-raw from your curl command
PAYLOAD = json.dumps({
  "city_ids": [
    "1"
  ],
  "pagination_data": {
    "@type": "type.googleapis.com/post_list.PaginationData",
    "last_post_date": "2025-08-16T08:36:24.999173Z",
    "page": 1,
    "layer_page": 1,
    "search_uid": "c8adcf5a-6e1c-40ce-9af1-47993fb59e2f",
    "cumulative_widgets_count": 25,
    "viewed_tokens": "H4sIAAAAAAAE/xSO206FMBBFf2hIgADq44C1gBeUBkFfyEihQWtsvSCcrz+Zt5W1s5OFZFSh029Acvl66XZAMk0wjBaQdIYf4QhIJiDUMSC9HLTiBGxSt3SANPmb+zrhl6+l4vtd82U9T6/Zw5o88US2fLxgUJ/7JgCpknP+a9jEtlXI4KOrtx+G3r4L7jFSpLJlc4jq2jH47jaKAGmZn8WRcc9ffyoKQAr15sgD0tCXqv4/BwAA///TFQzk1wAAAA==",
    "search_bookmark_info": {
      "search_hash": "96e8859aa1bb3a652e33a2d675f52fa0",
      "bookmark_state": {},
      "alert_state": {}
    }
  },
  "disable_recommendation": False,
  "map_state": {
    "camera_info": {
      "bbox": {}
    }
  },
  "search_data": {
    "form_data": {
      "data": {
        "bbox": {
          "repeated_float": {
            "value": [
              {
                "value": 51.3814
              },
              {
                "value": 35.6748657
              },
              {
                "value": 51.5442085
              },
              {
                "value": 35.7904663
              }
            ]
          }
        },
        "parking": {
          "boolean": {
            "value": True
          }
        },
        "category": {
          "str": {
            "value": "residential-sell"
          }
        },
        "size": {
          "number_range": {
            "minimum": "80"
          }
        },
        "price": {
          "number_range": {
            "minimum": "7000000000",
            "maximum": "9200000000"
          }
        },
        "districts": {
          "repeated_string": {
            "value": [
              "1035",
              "113",
              "116",
              "119",
              "125",
              "127",
              "134",
              "204",
              "208",
              "286",
              "291",
              "298",
              "299",
              "300",
              "301",
              "655",
              "658",
              "67",
              "84",
              "91",
              "936",
              "938",
              "965",
              "991"
            ]
          }
        }
      }
    },
    "server_payload": {
      "@type": "type.googleapis.com/widgets.SearchData.ServerPayload",
      "additional_form_data": {
        "data": {
          "sort": {
            "str": {
              "value": "sort_date"
            }
          }
        }
      }
    }
  }
})

# --- FUNCTIONS ---
def send_telegram_message(message_text, image_url, link,TELEGRAM_CHAT_ID):
    """Sends a message with an image and link to the Telegram bot."""
    if not all([TELEGRAM_BOT_TOKEN, TELEGRAM_CHAT_ID]):
        print("Telegram configuration is missing. Cannot send message.")
        return

    # Use the sendPhoto endpoint to send an image with a caption.
    # The caption can include Markdown to make the text clickable.
    url = f"https://api.telegram.org/bot{TELEGRAM_BOT_TOKEN}/sendPhoto"
    caption = f"{message_text}\n\n[مشاهده آگهی]({link})"
    
    payload = {
        'chat_id': TELEGRAM_CHAT_ID,
        'photo': image_url,
        'caption': caption,
        'parse_mode': 'Markdown'
    }

    try:
        response = requests.post(url, data=payload)
        response.raise_for_status()  # Raise an exception for bad status codes
        print("Message successfully sent to Telegram.")
    except requests.exceptions.RequestException as e:
        print(f"Error sending message to Telegram: {e}")

def check_for_new_posts():
    """Fetches new posts from the Divar API and sends notifications."""
    print("Checking for new posts...")
    
    headers = {
        'Content-Type': 'application/json',
        'Authorization': 'Bearer a3b49511182b41ccb48263dbfd09bc60.clm1md4ybIGz6E8w'
    }

    try:

        url = "https://api.divar.ir/v8/postlist/w/search"

        response = requests.request("POST", url, headers=headers, data=PAYLOAD)
        response.raise_for_status()  # Check for HTTP errors
        data = response.json()

        widgets = data.get('list_widgets', [])
        
        if not widgets:
            print("No new posts found in the response.")
            return

        for widget in widgets:
            data = widget.get('data', {})
            web_info = data.get('action', {}).get('payload', {}).get('web_info', {})
            token = data.get('token')
            title = web_info.get('title')
            district = web_info.get('district_persian')
            image_url = data.get('image_url')
            
            # The 'middle_description_text' is not always present at the top level.
            # It might be nested in the 'middle_description' object.
            price_text = data.get('middle_description_text', {})
            
            # Construct the message from the extracted data
            message = (
                f"عنوان: {title}\n"
                f"محله: {district}\n"
                f"قیمت: {price_text}"
            )
            
            link = f"https://divar.ir/v/{token}"
            
            if all([title, price_text, token]):
                for chat_id in TELEGRAM_CHAT_IDs:
                    send_telegram_message(message, image_url, link, chat_id)
            else:
                print("Skipping a post due to missing data.")

    except requests.exceptions.RequestException as e:
        print(f"An error occurred while fetching data: {e}")
    except json.JSONDecodeError as e:
        print(f"Error decoding JSON response: {e}")
    except Exception as e:
        print(f"An unexpected error occurred: {e}")

# --- MAIN LOOP ---
if __name__ == "__main__":
    while True:
        check_for_new_posts()
        print("Waiting for 1 hour...")
        time.sleep(60) # Sleep for 3600 seconds (1 hour)
