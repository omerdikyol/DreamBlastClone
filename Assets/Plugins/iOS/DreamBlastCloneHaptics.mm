#import <UIKit/UIKit.h>

extern "C"
{
    void DreamBlastClone_TriggerImpactHaptic(int style)
    {
        if (@available(iOS 10.0, *))
        {
            UIImpactFeedbackStyle feedbackStyle = UIImpactFeedbackStyleLight;

            switch (style)
            {
                case 1:
                    feedbackStyle = UIImpactFeedbackStyleMedium;
                    break;
                case 2:
                    feedbackStyle = UIImpactFeedbackStyleHeavy;
                    break;
                default:
                    feedbackStyle = UIImpactFeedbackStyleLight;
                    break;
            }

            UIImpactFeedbackGenerator* generator = [[UIImpactFeedbackGenerator alloc] initWithStyle:feedbackStyle];
            [generator prepare];
            [generator impactOccurred];
        }
    }
}
